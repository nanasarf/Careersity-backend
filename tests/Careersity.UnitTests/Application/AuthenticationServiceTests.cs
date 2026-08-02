using Careersity.Application.Abstractions.Authentication;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.Identity.Requests;
using Careersity.Application.Identity.Services;
using Careersity.Domain.Enums;
using Careersity.Domain.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Careersity.UnitTests.Application;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task Register_CreatesLearnerWithHashedPasswordAndTokens()
    {
        await using var db = TestCatalogContext.Create(); var harness = new Harness(db);
        var result = await harness.Service.RegisterAsync(Register(), "127.0.0.1", default);
        var user = await db.Users.Include(x => x.RefreshTokens).SingleAsync();
        user.Role.Should().Be(UserRole.Learner);
        user.PasswordHash.Should().Be("hashed:Valid!Pass1");
        user.LastLoginAtUtc.Should().NotBeNull();
        user.RefreshTokens.Should().ContainSingle();
        result.RefreshToken.Should().Be("refresh-1");
    }

    [Fact]
    public async Task Register_RejectsDuplicateNormalizedEmail()
    {
        await using var db = TestCatalogContext.Create(); var harness = new Harness(db);
        await harness.Service.RegisterAsync(Register(), null, default);
        var act = () => harness.Service.RegisterAsync(Register("PERSON@EXAMPLE.COM"), null, default);
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Login_UsesSameGenericFailureForUnknownWrongAndInactiveUsers()
    {
        await using var db = TestCatalogContext.Create(); var harness = new Harness(db);
        await harness.Service.RegisterAsync(Register(), null, default);
        var wrong = () => harness.Service.LoginAsync(new("person@example.com", "wrong"), null, default);
        var unknown = () => harness.Service.LoginAsync(new("unknown@example.com", "wrong"), null, default);
        var wrongError = await wrong.Should().ThrowAsync<AuthenticationFailedException>();
        var unknownError = await unknown.Should().ThrowAsync<AuthenticationFailedException>();
        wrongError.Which.Message.Should().Be(unknownError.Which.Message);
        var user = await db.Users.SingleAsync(); user.Deactivate(); await db.SaveChangesAsync();
        var inactive = () => harness.Service.LoginAsync(new("person@example.com", "Valid!Pass1"), null, default);
        (await inactive.Should().ThrowAsync<AuthenticationFailedException>()).Which.Message.Should().Be(wrongError.Which.Message);
    }

    [Fact]
    public async Task Refresh_RotatesAndReuseRevokesAllActiveSessions()
    {
        await using var db = TestCatalogContext.Create(); var harness = new Harness(db);
        var registered = await harness.Service.RegisterAsync(Register(), null, default);
        var rotated = await harness.Service.RefreshAsync(new(registered.RefreshToken), null, default);
        rotated.RefreshToken.Should().Be("refresh-2");
        var user = await db.Users.Include(x => x.RefreshTokens).SingleAsync();
        user.RefreshTokens.Single(x => x.TokenHash == "hash:refresh-1").RevokedAtUtc.Should().NotBeNull();
        var reuse = () => harness.Service.RefreshAsync(new(registered.RefreshToken), null, default);
        await reuse.Should().ThrowAsync<AuthenticationFailedException>();
        user.RefreshTokens.Should().OnlyContain(x => !x.IsActiveAt(harness.Clock.UtcNow));
    }

    [Fact]
    public async Task LogoutIsIdempotentAndRevokeAllRevokesSessions()
    {
        await using var db = TestCatalogContext.Create(); var harness = new Harness(db);
        var registered = await harness.Service.RegisterAsync(Register(), null, default);
        var user = await db.Users.SingleAsync(); harness.Current.Set(user);
        await harness.Service.LogoutAsync(new(registered.RefreshToken), null, default);
        await harness.Service.LogoutAsync(new("unknown"), null, default);
        await harness.Service.LoginAsync(new(user.Email, "Valid!Pass1"), null, default);
        await harness.Service.RevokeAllAsync(null, default);
        (await db.Users.Include(x => x.RefreshTokens).SingleAsync()).RefreshTokens.Should()
            .OnlyContain(x => !x.IsActiveAt(harness.Clock.UtcNow));
    }

    [Fact]
    public async Task PasswordChangeRequiresCurrentPasswordRevokesSessionsAndReturnsFreshPair()
    {
        await using var db = TestCatalogContext.Create(); var harness = new Harness(db);
        var registered = await harness.Service.RegisterAsync(Register(), null, default);
        var user = await db.Users.SingleAsync(); harness.Current.Set(user);
        var invalid = () => harness.Service.ChangePasswordAsync(new("wrong", "NewValid!2", "NewValid!2"), null, default);
        await invalid.Should().ThrowAsync<AuthenticationFailedException>();
        var changed = await harness.Service.ChangePasswordAsync(new("Valid!Pass1", "NewValid!2", "NewValid!2"), null, default);
        changed.RefreshToken.Should().Be("refresh-2");
        user.PasswordHash.Should().Be("hashed:NewValid!2");
        user.RefreshTokens.Single(x => x.TokenHash == $"hash:{registered.RefreshToken}").RevokedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ExpiredRefreshTokenIsRejected()
    {
        await using var db = TestCatalogContext.Create(); var harness = new Harness(db);
        var user = new User("expired@example.com", "Expired", "User", "hashed:Valid!Pass1", UserRole.Learner);
        user.AddRefreshToken(new RefreshToken(user.Id, "hash:expired", harness.Clock.UtcNow.AddDays(-2), harness.Clock.UtcNow.AddDays(-1)));
        db.Users.Add(user); await db.SaveChangesAsync();
        var act = () => harness.Service.RefreshAsync(new("expired"), null, default);
        await act.Should().ThrowAsync<AuthenticationFailedException>();
    }

    [Fact]
    public async Task ProfileServiceReadsCurrentUserAndUpdatesNamesOnly()
    {
        await using var db = TestCatalogContext.Create(); var harness = new Harness(db);
        await harness.Service.RegisterAsync(Register(), null, default);
        var user = await db.Users.SingleAsync(); harness.Current.Set(user);
        var profiles = new UserProfileService(db, harness.Current);
        (await profiles.GetAsync(default)).Email.Should().Be(user.Email);
        var updated = await profiles.UpdateAsync(new("Updated", "Name"), default);
        updated.FullName.Should().Be("Updated Name");
        updated.Email.Should().Be(user.Email); updated.Role.Should().Be(UserRole.Learner);
    }

    private static RegisterRequest Register(string email = "person@example.com") => new(email, "First", "Last", "Valid!Pass1", "Valid!Pass1");

    private sealed class Harness
    {
        public Harness(TestCatalogContext db)
        {
            Service = new AuthenticationService(db, new FakePasswords(), new FakeAccessTokens(Clock), Tokens,
                Current, Clock, NullLogger<AuthenticationService>.Instance);
        }
        public FakeClock Clock { get; } = new();
        public FakeCurrentUser Current { get; } = new();
        public FakeRefreshTokens Tokens { get; } = new();
        public AuthenticationService Service { get; }
    }
    private sealed class FakePasswords : IPasswordService
    { public string Hash(string password) => $"hashed:{password}"; public bool Verify(string hash, string password) => hash == Hash(password); }
    private sealed class FakeAccessTokens(FakeClock clock) : IAccessTokenService
    { public AccessTokenResult Create(User user) => new($"access-{user.Id}", clock.UtcNow.AddMinutes(15)); }
    private sealed class FakeRefreshTokens : IRefreshTokenService
    {
        private int _next; public TimeSpan Lifetime => TimeSpan.FromDays(14);
        public RefreshTokenResult Generate() { var raw = $"refresh-{++_next}"; return new(raw, Hash(raw)); }
        public string Hash(string rawToken) => $"hash:{rawToken}";
    }
    private sealed class FakeClock : IDateTimeProvider { public DateTimeOffset UtcNow { get; } = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero); }
    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? UserId { get; private set; } public string? Email { get; private set; }
        public UserRole? Role { get; private set; } public bool IsAuthenticated => UserId.HasValue;
        public void Set(User user) { UserId = user.Id; Email = user.Email; Role = user.Role; }
    }
}
