using Careersity.Application;
using Careersity.Infrastructure.Persistence;
using FluentAssertions;
using Xunit;

namespace Careersity.IntegrationTests.Architecture;

public sealed class ProjectArchitectureTests
{
    [Fact]
    public void Application_DoesNotReferenceInfrastructure()
    {
        typeof(DependencyInjection).Assembly.GetReferencedAssemblies()
            .Select(x => x.Name).Should().NotContain("Careersity.Infrastructure");
    }

    [Fact]
    public void Infrastructure_ContainsPersistenceAndReferencesInwardOnly()
    {
        var assembly = typeof(CareersityDbContext).Assembly;
        assembly.GetTypes().Should().Contain(typeof(CareersityDbContext));
        var careersityReferences = assembly.GetReferencedAssemblies().Select(x => x.Name)
            .Where(x => x?.StartsWith("Careersity.", StringComparison.Ordinal) == true)
            .ToArray();
        careersityReferences.Should().Contain("Careersity.Domain");
        careersityReferences.Should().OnlyContain(x => x == "Careersity.Application" || x == "Careersity.Domain");
    }
}
