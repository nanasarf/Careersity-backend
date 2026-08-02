using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Careersity.IntegrationTests;

public sealed class HealthEndpointTests
{
    private readonly HttpClient _client;

    public HealthEndpointTests()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Jwt:SigningKey"] = TestApiFactory.SigningKey })));
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        using var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReadinessWithoutDatabase_ReturnsServiceUnavailable()
    {
        using var response = await _client.GetAsync("/health/ready");
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync()).ToLowerInvariant().Should().NotContain("password");
    }

    [Fact]
    public async Task CorrelationHeader_IsGeneratedOrPreservedAndMatchesProblemDetails()
    {
        using var generated = await _client.GetAsync("/health");
        generated.Headers.Contains("X-Correlation-ID").Should().BeTrue();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/learning-providers");
        request.Headers.Add("X-Correlation-ID", "client-correlation-123");
        using var response = await _client.SendAsync(request);
        response.Headers.GetValues("X-Correlation-ID").Single().Should().Be("client-correlation-123");
        (await response.Content.ReadAsStringAsync()).Should().Contain("client-correlation-123");
    }
}
