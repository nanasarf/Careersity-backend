using Careersity.Application.Abstractions.Authentication;
using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.Identity.Dtos;
using Careersity.Application.Identity.Requests;
using Careersity.Domain.Enums;
using Careersity.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Careersity.Application.Identity.Services;

public sealed class AuthenticationService(ICareersityDbContext db, IPasswordService passwords,
    IAccessTokenService accessTokens, IRefreshTokenService refreshTokens, ICurrentUser currentUser,
    IDateTimeProvider clock, ILogger<AuthenticationService> logger) : IAuthenticationService
{
    private const string GenericFailure = "Invalid credentials or token.";

    public async Task<AuthenticationResultDto> RegisterAsync(RegisterRequest request, string? ip, CancellationToken cancellationToken)
    {
        var normalized = User.NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(x => x.NormalizedEmail == normalized, cancellationToken))
            throw new ConflictException("An account with this email already exists.");
        var user = new User(request.Email, request.FirstName, request.LastName, passwords.Hash(request.Password), UserRole.Learner);
        db.Users.Add(user);
        var result = IssueTokens(user, ip); user.RecordSuccessfulLogin(clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("User registration and initial login succeeded for user {UserId}", user.Id);
        return result;
    }

    public async Task<AuthenticationResultDto> LoginAsync(LoginRequest request, string? ip, CancellationToken cancellationToken)
    {
        var normalized = User.NormalizeEmail(request.Email);
        var user = await db.Users.Include(x => x.RefreshTokens).SingleOrDefaultAsync(x => x.NormalizedEmail == normalized, cancellationToken);
        if (user is null || !user.IsActive || !passwords.Verify(user.PasswordHash, request.Password))
        { logger.LogWarning("Authentication attempt failed"); throw new AuthenticationFailedException(GenericFailure); }
        user.RecordSuccessfulLogin(clock.UtcNow); var result = IssueTokens(user, ip);
        await db.SaveChangesAsync(cancellationToken); logger.LogInformation("Login succeeded for user {UserId}", user.Id); return result;
    }

    public async Task<AuthenticationResultDto> RefreshAsync(RefreshAccessTokenRequest request, string? ip, CancellationToken cancellationToken)
    {
        var hash = refreshTokens.Hash(request.RefreshToken);
        var user = await db.Users.Include(x => x.RefreshTokens).SingleOrDefaultAsync(x => x.RefreshTokens.Any(t => t.TokenHash == hash), cancellationToken)
            ?? throw new AuthenticationFailedException(GenericFailure);
        var oldToken = user.RefreshTokens.Single(x => x.TokenHash == hash); var now = clock.UtcNow;
        if (!user.IsActive) throw new AuthenticationFailedException(GenericFailure);
        if (oldToken.RevokedAtUtc is not null)
        {
            user.RevokeAllActiveRefreshTokens(now, ip); await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Refresh-token reuse detected for user {UserId}; all sessions revoked", user.Id);
            throw new AuthenticationFailedException(GenericFailure);
        }
        if (!oldToken.IsActiveAt(now)) throw new AuthenticationFailedException(GenericFailure);
        var generated = refreshTokens.Generate();
        var replacement = new RefreshToken(user.Id, generated.TokenHash, now, now.Add(refreshTokens.Lifetime), ip);
        user.AddRefreshToken(replacement); db.RefreshTokens.Add(replacement);
        user.RevokeRefreshToken(oldToken.Id, now, ip, replacement.Id);
        var access = accessTokens.Create(user); await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Refresh token rotated for user {UserId}", user.Id);
        return BuildResult(user, access, generated.RawToken, replacement.ExpiresAtUtc);
    }

    public async Task LogoutAsync(LogoutRequest request, string? ip, CancellationToken cancellationToken)
    {
        var userId = RequireUserId(); var hash = refreshTokens.Hash(request.RefreshToken);
        var user = await db.Users.Include(x => x.RefreshTokens).SingleAsync(x => x.Id == userId, cancellationToken);
        var token = user.RefreshTokens.SingleOrDefault(x => x.TokenHash == hash);
        if (token is not null) user.RevokeRefreshToken(token.Id, clock.UtcNow, ip);
        await db.SaveChangesAsync(cancellationToken); logger.LogInformation("Logout completed for user {UserId}", userId);
    }

    public async Task RevokeAllAsync(string? ip, CancellationToken cancellationToken)
    {
        var userId = RequireUserId(); var user = await db.Users.Include(x => x.RefreshTokens).SingleAsync(x => x.Id == userId, cancellationToken);
        user.RevokeAllActiveRefreshTokens(clock.UtcNow, ip); await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthenticationResultDto> ChangePasswordAsync(ChangeMyPasswordRequest request, string? ip, CancellationToken cancellationToken)
    {
        var userId = RequireUserId(); var user = await db.Users.Include(x => x.RefreshTokens).SingleAsync(x => x.Id == userId, cancellationToken);
        if (!passwords.Verify(user.PasswordHash, request.CurrentPassword)) throw new AuthenticationFailedException(GenericFailure);
        user.ChangePasswordHash(passwords.Hash(request.NewPassword)); user.RevokeAllActiveRefreshTokens(clock.UtcNow, ip);
        var result = IssueTokens(user, ip); await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Password changed and sessions revoked for user {UserId}", user.Id); return result;
    }

    private AuthenticationResultDto IssueTokens(User user, string? ip)
    {
        var now = clock.UtcNow; var generated = refreshTokens.Generate();
        var refresh = new RefreshToken(user.Id, generated.TokenHash, now, now.Add(refreshTokens.Lifetime), ip);
        user.AddRefreshToken(refresh); db.RefreshTokens.Add(refresh);
        return BuildResult(user, accessTokens.Create(user), generated.RawToken, refresh.ExpiresAtUtc);
    }
    private static AuthenticationResultDto BuildResult(User user, AccessTokenResult access, string rawRefresh, DateTimeOffset refreshExpires) =>
        new(ToUserDto(user), access.Token, access.ExpiresAtUtc, rawRefresh, refreshExpires);
    internal static AuthenticatedUserDto ToUserDto(User user) => new(user.Id, user.Email, user.FirstName, user.LastName, $"{user.FirstName} {user.LastName}", user.Role);
    private Guid RequireUserId() => currentUser.IsAuthenticated && currentUser.UserId.HasValue ? currentUser.UserId.Value : throw new UnauthorizedException();
}
