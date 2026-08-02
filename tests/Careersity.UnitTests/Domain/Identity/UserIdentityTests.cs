using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;
using Careersity.Domain.Identity;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Domain.Identity;

public sealed class UserIdentityTests
{
    [Fact]
    public void User_NormalizesEmailAndStartsActiveWithControlledRole()
    {
        var user = CreateUser(UserRole.Administrator);
        user.Email.Should().Be("person@example.com");
        user.NormalizedEmail.Should().Be("PERSON@EXAMPLE.COM");
        user.Role.Should().Be(UserRole.Administrator);
        user.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Last")]
    [InlineData("First", " ")]
    public void User_RejectsMissingNames(string firstName, string lastName) =>
        FluentActions.Invoking(() => new User("person@example.com", firstName, lastName, "hash", UserRole.Learner))
            .Should().Throw<ArgumentException>();

    [Fact]
    public void User_RecordsLoginAndCanBeDeactivated()
    {
        var user = CreateUser(); var loginAt = DateTimeOffset.UtcNow;
        user.RecordSuccessfulLogin(loginAt); user.Deactivate();
        user.LastLoginAtUtc.Should().Be(loginAt);
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void User_RejectsDuplicateRefreshTokenHash()
    {
        var user = CreateUser(); var now = DateTimeOffset.UtcNow;
        user.AddRefreshToken(new RefreshToken(user.Id, "HASH", now, now.AddDays(1)));
        FluentActions.Invoking(() => user.AddRefreshToken(new RefreshToken(user.Id, "HASH", now, now.AddDays(1))))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void RefreshToken_RequiresFutureExpiration()
    {
        var now = DateTimeOffset.UtcNow;
        FluentActions.Invoking(() => new RefreshToken(Guid.NewGuid(), "HASH", now, now))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void RefreshToken_ActiveRevokedExpiredAndReplacementBehaviorIsConsistent()
    {
        var now = DateTimeOffset.UtcNow; var replacementId = Guid.NewGuid();
        var token = new RefreshToken(Guid.NewGuid(), "HASH", now, now.AddHours(1));
        token.IsActiveAt(now).Should().BeTrue();
        token.IsActiveAt(now.AddHours(2)).Should().BeFalse();
        token.Revoke(now.AddMinutes(1), replacedByTokenId: replacementId);
        token.IsActiveAt(now.AddMinutes(2)).Should().BeFalse();
        token.ReplacedByTokenId.Should().Be(replacementId);
        token.Revoke(now.AddMinutes(3));
        token.RevokedAtUtc.Should().Be(now.AddMinutes(1));
    }

    private static User CreateUser(UserRole role = UserRole.Learner) =>
        new("person@example.com", "First", "Last", "hash", role);
}
