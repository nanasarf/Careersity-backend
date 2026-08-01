using System.Reflection;
using Careersity.Domain;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Architecture;

public sealed class DomainArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(AssemblyMarker).Assembly;

    [Fact]
    public void Domain_DoesNotReferenceOuterProjectsOrFrameworks()
    {
        var forbiddenReferences = DomainAssembly.GetReferencedAssemblies()
            .Select(x => x.Name)
            .Where(x => x is not null &&
                (x.StartsWith("Careersity.", StringComparison.Ordinal)
                 || x.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                 || x.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)));

        forbiddenReferences.Should().BeEmpty();
    }

    [Fact]
    public void Domain_DoesNotUsePersistenceAttributes()
    {
        var persistenceAttributes = DomainAssembly.GetTypes()
            .SelectMany(type => type.GetCustomAttributesData()
                .Concat(type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .SelectMany(property => property.GetCustomAttributesData())))
            .Where(attribute => attribute.AttributeType.Namespace is
                "System.ComponentModel.DataAnnotations" or "System.ComponentModel.DataAnnotations.Schema"
                || attribute.AttributeType.Namespace?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true);

        persistenceAttributes.Should().BeEmpty();
    }
}
