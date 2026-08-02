using System.Net.Http.Headers;
using System.Net.Http.Json;
using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Identity.Dtos;
using Careersity.Application.Identity.Requests;
using Careersity.Domain.Enums;
using Careersity.Domain.Identity;
using Careersity.Infrastructure.Persistence;
using Careersity.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Careersity.IntegrationTests;

internal static class TestApiFactory
{
    internal const string SigningKey = "CAREERSITY-INTEGRATION-TEST-KEY-ONLY-0123456789abcdef";
    internal const string AdminPassword = "AdminValid!1";
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };

    public static WebApplicationFactory<Program> Create(PostgreSqlFixture database) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:CareersityDatabase"] = database.ConnectionString,
                    ["Jwt:Issuer"] = "Careersity.Tests", ["Jwt:Audience"] = "Careersity.Tests.Client",
                    ["Jwt:SigningKey"] = SigningKey, ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                    ["Jwt:RefreshTokenLifetimeDays"] = "14"
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ICareersityDbContext>(); services.RemoveAll<CareersityDbContext>();
                services.RemoveAll<DbContextOptions<CareersityDbContext>>();
                services.AddDbContext<CareersityDbContext>(options => options.UseNpgsql(database.ConnectionString));
                services.AddScoped<ICareersityDbContext>(provider => provider.GetRequiredService<CareersityDbContext>());
            });
        });

    public static async Task<(WebApplicationFactory<Program> Factory, HttpClient Client)> CreateAdministratorClientAsync(PostgreSqlFixture database)
    {
        var email = $"admin-{Guid.NewGuid():N}@example.com";
        var hasher = new PasswordHasher<User>();
        var administrator = new User(email, "Test", "Administrator", hasher.HashPassword(null!, AdminPassword), UserRole.Administrator);
        await using (var context = database.CreateContext()) { context.Users.Add(administrator); await context.SaveChangesAsync(); }
        var factory = Create(database); var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, AdminPassword));
        response.EnsureSuccessStatusCode();
        var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResultDto>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authentication!.AccessToken);
        return (factory, client);
    }
}
