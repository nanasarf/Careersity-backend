using Careersity.Application;
using Careersity.Infrastructure.Persistence;
using Careersity.Application.Abstractions.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Careersity.Application.Identity.Requests;
using Careersity.Api.Infrastructure;
using Careersity.Application.CurriculumActivities.Dtos;
using Careersity.Application.LearningProgress.Dtos;
using Careersity.Application.LearningProgress.Requests;
using Careersity.Application.AssessmentAttempts.Dtos;
using Careersity.Application.AssessmentAttempts.Requests;
using Careersity.Application.ExternalLearning.Requests;
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
    public void CurriculumActivityControllersHaveCorrectAuthorizationBoundaries()
    {
        var api = typeof(Program).Assembly;
        var administrators = api.GetTypes().Where(x => x.Name is "AdminAssessmentsController" or "AdminProjectsController").ToArray();
        administrators.Should().HaveCount(2);
        administrators.Should().OnlyContain(x => x.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>().Any(a => a.Policy == SecurityPolicies.AdministratorOnly));
        administrators.Should().OnlyContain(x => !x.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any());

        var publicController = api.GetTypes().Single(x => x.Name == "PublicCourseActivitiesController");
        publicController.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Should().NotBeEmpty();
        publicController.GetCustomAttributes(typeof(AuthorizeAttribute), true).Should().BeEmpty();
    }

    [Fact]
    public void PublicAssessmentContractCannotExposeAnswerCorrectness()
    {
        var forbidden = new[] { "IsCorrect", "CorrectAnswer", "CorrectAnswerId", "AnswerKey" };
        typeof(PublicAssessmentSummaryDto).GetProperties().Select(x => x.Name).Should().NotIntersectWith(forbidden);
    }

    [Fact]
    public void LearnerProgressBoundaryRequiresAuthenticationWithoutAdministratorPolicy()
    {
        var controller = typeof(Program).Assembly.GetTypes().Single(x => x.Name == "LearnerProgressController");
        var authorization = controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().ToArray();
        authorization.Should().ContainSingle(); authorization.Single().Policy.Should().BeNull();
        controller.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Should().BeEmpty();
        typeof(EnrollInCareerRequest).GetProperties().Select(x => x.Name).Should().NotContain("UserId");
    }

    [Fact]
    public void LearningProgressContractsContainNoSensitiveSecurityOrAnswerKeyFields()
    {
        var forbidden = new[] { "UserId", "Password", "PasswordHash", "Token", "TokenHash", "IsCorrect", "AnswerKey" };
        var types = typeof(CareerEnrollmentDetailDto).Assembly.GetTypes().Where(x => x.Namespace == typeof(CareerEnrollmentDetailDto).Namespace);
        types.SelectMany(x => x.GetProperties()).Select(x => x.Name).Should().NotIntersectWith(forbidden);
    }

    [Fact]
    public void LearnerAssessmentBoundaryIsAuthenticatedAndRequestsNeverAcceptUserId()
    {
        var controller = typeof(Program).Assembly.GetTypes().Single(x => x.Name == "LearnerAssessmentsController");
        controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).Should().ContainSingle();
        controller.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Should().BeEmpty();
        new[] { typeof(SaveAssessmentResponseRequest), typeof(SaveAssessmentResponsesRequest), typeof(SubmitAssessmentAttemptRequest) }
            .SelectMany(x => x.GetProperties()).Select(x => x.Name).Should().NotContain("UserId");
    }

    [Fact]
    public void AssessmentStartContractsAndLearnerDtosNeverExposeAnswerKeys()
    {
        var forbidden = new[] { "IsCorrect", "CorrectAnswer", "CorrectAnswerId", "CorrectAnswerOptionIds", "AnswerKey", "PasswordHash", "TokenHash" };
        new[] { typeof(AssessmentAttemptStartDto), typeof(LearnerAssessmentQuestionDto), typeof(LearnerAnswerOptionDto), typeof(LearnerAssessmentSummaryDto) }
            .SelectMany(x => x.GetProperties()).Select(x => x.Name).Should().NotIntersectWith(forbidden);
        typeof(LearnerAssessmentResponseDto).GetProperties().Select(x => x.Name)
            .Should().NotContain(["CorrectAnswerId", "CorrectAnswerOptionIds", "AnswerKey"]);
    }

    [Fact]
    public void ExternalLearningAuthorizationAndRequestBoundariesAreCorrect()
    {
        var api = typeof(Program).Assembly;
        var admins = api.GetTypes().Where(x => x.Name.StartsWith("Admin", StringComparison.Ordinal) && x.Name.Contains("External", StringComparison.Ordinal) || x.Name is "AdminLearningProvidersController" or "AdminInstructorsController").ToArray();
        admins.Should().NotBeEmpty(); admins.Should().OnlyContain(x => x.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Any(a => a.Policy == SecurityPolicies.AdministratorOnly));
        var publicController = api.GetTypes().Single(x => x.Name == "PublicExternalLearningController"); publicController.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Should().NotBeEmpty();
        var learner = api.GetTypes().Single(x => x.Name == "LearnerExternalResourcesController"); learner.GetCustomAttributes(typeof(AuthorizeAttribute), true).Should().NotBeEmpty(); learner.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Should().BeEmpty();
        typeof(CreateLearningProviderRequest).Assembly.GetTypes().Where(x => x.Namespace == typeof(CreateLearningProviderRequest).Namespace).SelectMany(x => x.GetProperties()).Select(x => x.Name).Should().NotContain("UserId");
    }

    [Fact]
    public void ExternalLearningIntroducesNoRemoteFetchingClient()
    {
        typeof(DependencyInjection).Assembly.GetTypes().Should().NotContain(x => (x.Namespace ?? string.Empty).Contains("ExternalLearning", StringComparison.Ordinal) && (x.Name.Contains("HttpClient", StringComparison.Ordinal) || x.Name.Contains("Scraper", StringComparison.Ordinal)));
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
