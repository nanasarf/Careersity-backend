using Careersity.Domain.Enums;
using Careersity.Domain.Identity;

namespace Careersity.Application.Abstractions.Authentication;

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAtUtc);
public sealed record RefreshTokenResult(string RawToken, string TokenHash);

public interface IAccessTokenService { AccessTokenResult Create(User user); }
public interface IRefreshTokenService { TimeSpan Lifetime { get; } RefreshTokenResult Generate(); string Hash(string rawToken); }
public interface IPasswordService { string Hash(string password); bool Verify(string passwordHash, string password); }
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    UserRole? Role { get; }
    bool IsAuthenticated { get; }
}
public interface IDateTimeProvider { DateTimeOffset UtcNow { get; } }
