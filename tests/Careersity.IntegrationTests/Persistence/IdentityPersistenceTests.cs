using Careersity.Domain.Enums;
using Careersity.Domain.Identity;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Careersity.Application.Common.Exceptions;
using Xunit;

namespace Careersity.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class IdentityPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task UserAndPrivateRefreshTokenCollection_PersistAndReload()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var user = new User($"person-{suffix}@example.com", "First", "Last", "HASHED-PASSWORD", UserRole.Administrator);
        var rawToken = $"raw-{suffix}";
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
        user.AddRefreshToken(new RefreshToken(user.Id, hash, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1)));
        await using (var context = fixture.CreateContext()) { context.Users.Add(user); await context.SaveChangesAsync(); }
        await using var verify = fixture.CreateContext();
        var loaded = await verify.Users.Include(x => x.RefreshTokens).SingleAsync(x => x.Id == user.Id);
        loaded.Role.Should().Be(UserRole.Administrator);
        loaded.RefreshTokens.Should().ContainSingle(x => x.TokenHash == hash);
        loaded.RefreshTokens.Single().TokenHash.Should().NotBe(rawToken);
    }

    [Fact]
    public async Task NormalizedEmailAndTokenHash_AreUniquelyConstrained()
    {
        var suffix = Guid.NewGuid().ToString("N");
        await using var context = fixture.CreateContext();
        context.Users.Add(new User($"Case-{suffix}@example.com", "One", "User", "HASH", UserRole.Learner));
        context.Users.Add(new User($"case-{suffix}@example.com", "Two", "User", "HASH", UserRole.Learner));
        var act = () => context.SaveChangesAsync();
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task RefreshTokenExpirationConstraint_RejectsInvalidDirectSql()
    {
        await using var context = fixture.CreateContext();
        var user = new User($"constraint-{Guid.NewGuid():N}@example.com", "Test", "User", "HASH", UserRole.Learner);
        context.Users.Add(user); await context.SaveChangesAsync();
        var sql = "INSERT INTO \"RefreshTokens\" (\"Id\", \"UserId\", \"TokenHash\", \"CreatedAtUtc\", \"ExpiresAtUtc\") VALUES (@id, @user, @hash, @created, @expires)";
        await using var command = new NpgsqlCommand(sql, (NpgsqlConnection)context.Database.GetDbConnection());
        await context.Database.OpenConnectionAsync();
        var now = DateTimeOffset.UtcNow;
        command.Parameters.AddWithValue("id", Guid.NewGuid()); command.Parameters.AddWithValue("user", user.Id);
        command.Parameters.AddWithValue("hash", Guid.NewGuid().ToString("N")); command.Parameters.AddWithValue("created", now);
        command.Parameters.AddWithValue("expires", now.AddMinutes(-1));
        var act = () => command.ExecuteNonQueryAsync();
        await act.Should().ThrowAsync<PostgresException>().Where(x => x.SqlState == PostgresErrorCodes.CheckViolation);
    }
}
