using Careersity.Application;
using Careersity.Infrastructure.Persistence;
using Careersity.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Careersity.Application.Identity.Requests;
using Careersity.Api.Infrastructure;
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

    [Fact]
    public void AdminControllers_RequireAdministratorPolicyAndAreNotAnonymous()
    {
        var adminControllers = typeof(Program).Assembly.GetTypes()
            .Where(x => typeof(ControllerBase).IsAssignableFrom(x) && x.Name.StartsWith("Admin", StringComparison.Ordinal));
        adminControllers.Should().NotBeEmpty();
        adminControllers.Should().OnlyContain(x => x.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Any(a => a.Policy == SecurityPolicies.AdministratorOnly));
        adminControllers.Should().OnlyContain(x => !x.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any());
    }

    [Fact]
    public void PublicRegistrationCannotSelectRole() =>
        typeof(RegisterRequest).GetProperties().Select(x => x.Name).Should().NotContain("Role");

    [Fact]
    public void PublicLearningContentControllersAreAnonymousAndDoNotExposeDomainReturnTypes()
    {
        var publicControllers = typeof(Program).Assembly.GetTypes().Where(x =>
            x.Name is "PublicSkillsController" or "PublicCoursesController").ToArray();
        publicControllers.Should().HaveCount(2);
        publicControllers.Should().OnlyContain(x => x.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any());
        publicControllers.Should().OnlyContain(x => !x.GetCustomAttributes(typeof(AuthorizeAttribute), true).Any());
        publicControllers.SelectMany(x => x.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)
            .Where(method => method.DeclaringType == x)).Select(x => x.ReturnType)
            .Should().NotContain(type => type.Assembly == typeof(Careersity.Domain.Courses.Course).Assembly);
    }

    [Fact]
    public void SecurityImplementationsRemainOutsideDomain()
    {
        var domain = typeof(Careersity.Domain.Identity.User).Assembly;
        domain.GetReferencedAssemblies().Select(x => x.Name).Should().NotContain([
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore.Authentication.JwtBearer", "System.IdentityModel.Tokens.Jwt"]);
        domain.GetTypes().Should().NotContain(x => x.Name.Contains("PasswordHasher", StringComparison.Ordinal)
            || x.Name.Contains("AccessTokenService", StringComparison.Ordinal));
        typeof(Careersity.Domain.Identity.RefreshToken).GetProperties().Select(x => x.Name).Should().NotContain("Token");
    }
}
