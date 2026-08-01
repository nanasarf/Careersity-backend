using Careersity.Application;
using Careersity.Infrastructure.Persistence;
using Careersity.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Mvc;
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

    [Fact]
    public void Infrastructure_ImplementsApplicationPersistenceAbstraction() =>
        typeof(ICareersityDbContext).IsAssignableFrom(typeof(CareersityDbContext)).Should().BeTrue();

    [Fact]
    public void ApiControllers_DoNotDependOnCareersityDbContext()
    {
        var controllerTypes = typeof(Program).Assembly.GetTypes().Where(x => typeof(ControllerBase).IsAssignableFrom(x));
        controllerTypes.SelectMany(x => x.GetFields(System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
            .Select(x => x.FieldType).Should().NotContain(typeof(CareersityDbContext));
    }

    [Fact]
    public void Solution_DoesNotIntroduceGenericRepositories()
    {
        var types = typeof(CareersityDbContext).Assembly.GetTypes()
            .Concat(typeof(DependencyInjection).Assembly.GetTypes());
        types.Should().NotContain(x => x.Name.Contains("GenericRepository", StringComparison.Ordinal)
            || x.Name == "IRepository`1");
    }
}
