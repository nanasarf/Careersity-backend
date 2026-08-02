using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Careersity.Application.Identity.Dtos;
using Careersity.Application.Identity.Requests;
using Careersity.Domain.Enums;
using Careersity.IntegrationTests.Persistence;
using FluentAssertions;
using Xunit;

namespace Careersity.IntegrationTests.Identity;

[Collection(PostgreSqlCollection.Name)]
public sealed class AuthenticationApiTests(PostgreSqlFixture database)
{
    [Fact]
    public async Task CompleteAuthenticationFlow_RotatesLogsOutAndChangesPassword()
    {
        using var factory = TestApiFactory.Create(database); using var client = factory.CreateClient();
        var email = $"learner-{Guid.NewGuid():N}@example.com";
        var registration = await RegisterAsync(client, email);
        registration.User.Role.Should().Be(UserRole.Learner);
        Use(client, registration.AccessToken);
        var profile = await client.GetFromJsonAsync<UserProfileDto>("/api/users/me", TestApiFactory.JsonOptions);
        profile!.Email.Should().Be(email);

        client.DefaultRequestHeaders.Authorization = null;
        var rotatedResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshAccessTokenRequest(registration.RefreshToken));
        rotatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await rotatedResponse.Content.ReadFromJsonAsync<AuthenticationResultDto>(TestApiFactory.JsonOptions);
        rotated!.RefreshToken.Should().NotBe(registration.RefreshToken);
        (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshAccessTokenRequest(registration.RefreshToken))).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshAccessTokenRequest(rotated.RefreshToken))).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        var login = await LoginAsync(client, email, "Valid!Pass1"); Use(client, login.AccessToken);
        (await client.PostAsJsonAsync("/api/auth/logout", new LogoutRequest(login.RefreshToken))).StatusCode.Should().Be(HttpStatusCode.NoContent);
        client.DefaultRequestHeaders.Authorization = null;
        (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshAccessTokenRequest(login.RefreshToken))).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        login = await LoginAsync(client, email, "Valid!Pass1"); Use(client, login.AccessToken);
        var changedResponse = await client.PostAsJsonAsync("/api/auth/change-password",
            new ChangeMyPasswordRequest("Valid!Pass1", "NewValid!Pass2", "NewValid!Pass2"));
        changedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        client.DefaultRequestHeaders.Authorization = null;
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Valid!Pass1"))).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "NewValid!Pass2"))).StatusCode
            .Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminPolicy_ReturnsProblemDetailsForAnonymousAndLearnerAndAllowsAdministrator()
    {
        using var factory = TestApiFactory.Create(database); using var anonymous = factory.CreateClient();
        var unauthorized = await anonymous.GetAsync("/api/admin/career-categories");
        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await unauthorized.Content.ReadAsStringAsync()).Should().Contain("traceId");

        var learner = await RegisterAsync(anonymous, $"learner-{Guid.NewGuid():N}@example.com");
        Use(anonymous, learner.AccessToken);
        var forbidden = await anonymous.GetAsync("/api/admin/career-categories");
        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await forbidden.Content.ReadAsStringAsync()).Should().Contain("traceId");
        anonymous.DefaultRequestHeaders.Authorization = null;
        (await anonymous.GetAsync("/api/careers")).StatusCode.Should().Be(HttpStatusCode.OK);

        var authenticated = await TestApiFactory.CreateAdministratorClientAsync(database);
        using var adminFactory = authenticated.Factory; using var admin = authenticated.Client;
        (await admin.GetAsync("/api/admin/career-categories")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthenticationErrors_AreSafeAndStructured()
    {
        using var factory = TestApiFactory.Create(database); using var client = factory.CreateClient();
        var email = $"errors-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(client, email);
        (await client.PostAsJsonAsync("/api/auth/register", Registration(email))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        var invalid = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("bad", "", "", "short", "different"));
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await invalid.Content.ReadAsStringAsync()).Should().Contain("errors").And.Contain("traceId");
        var wrong = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Wrong!Pass1"));
        wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await wrong.Content.ReadAsStringAsync()).Should().Contain("Invalid credentials or token.").And.NotContain(email);
        var refresh = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshAccessTokenRequest("not-a-token"));
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await refresh.Content.ReadAsStringAsync()).Should().Contain("traceId");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        var invalidAccess = await client.GetAsync("/api/users/me");
        invalidAccess.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await invalidAccess.Content.ReadAsStringAsync()).Should().Contain("traceId");
    }

    [Fact]
    public async Task LoginRateLimit_ReturnsStructured429()
    {
        using var factory = TestApiFactory.Create(database); using var client = factory.CreateClient();
        HttpResponseMessage? response = null;
        for (var attempt = 0; attempt < 11; attempt++)
            response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("missing@example.com", "Wrong!Pass1"));
        response!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await response.Content.ReadAsStringAsync()).Should().Contain("traceId");
    }

    [Fact]
    public async Task InactiveUserCannotLoginOrRefresh()
    {
        using var factory = TestApiFactory.Create(database); using var client = factory.CreateClient();
        var email = $"inactive-{Guid.NewGuid():N}@example.com";
        var authentication = await RegisterAsync(client, email);
        await using (var context = database.CreateContext())
        {
            var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(context.Users, x => x.Email == email);
            user.Deactivate(); await context.SaveChangesAsync();
        }
        (await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Valid!Pass1"))).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/auth/refresh", new RefreshAccessTokenRequest(authentication.RefreshToken))).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task<AuthenticationResultDto> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", Registration(email));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<AuthenticationResultDto>(TestApiFactory.JsonOptions))!;
    }
    private static RegisterRequest Registration(string email) => new(email, "Test", "Learner", "Valid!Pass1", "Valid!Pass1");
    private static async Task<AuthenticationResultDto> LoginAsync(HttpClient client, string email, string password)
    {
        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<AuthenticationResultDto>(TestApiFactory.JsonOptions))!;
    }
    private static void Use(HttpClient client, string token) => client.DefaultRequestHeaders.Authorization = new("Bearer", token);
}
