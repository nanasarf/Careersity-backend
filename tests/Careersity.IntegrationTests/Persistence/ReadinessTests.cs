using System.Net; using FluentAssertions; using Xunit;
namespace Careersity.IntegrationTests.Persistence;
[Collection(PostgreSqlCollection.Name)]
public sealed class ReadinessTests(PostgreSqlFixture fixture)
{
 [Fact]public async Task MigratedPostgreSqlIsReady(){await using var factory=TestApiFactory.Create(fixture);using var client=factory.CreateClient();using var response=await client.GetAsync("/health/ready");response.StatusCode.Should().Be(HttpStatusCode.OK);}
}
