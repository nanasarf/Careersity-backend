using System.Net;
using System.Net.Http.Json;
using Careersity.Application.CareerCatalog.Dtos;
using Careersity.Application.CareerCatalog.Requests;
using Careersity.Application.Common.Models;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using Careersity.Domain.Skills;
using Careersity.IntegrationTests.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Careersity.Application.Abstractions.Persistence;
using Careersity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Careersity.IntegrationTests.CareerCatalog;

[Collection(PostgreSqlCollection.Name)]
public sealed class CareerCatalogApiTests(PostgreSqlFixture database)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task CompleteAdminFlow_ProducesPublishedCareerAndOrderedPathway()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var (skill, course) = await CreatePublishedDependenciesAsync(suffix);
        var authenticated = await TestApiFactory.CreateAdministratorClientAsync(database);
        using var factory = authenticated.Factory; using var client = authenticated.Client;

        var categoryResponse = await client.PostAsJsonAsync("/api/admin/career-categories",
            new CreateCareerCategoryRequest("Technology", $"technology-{suffix}", "Technology careers"));
        categoryResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CareerCategoryDto>(JsonOptions); category.Should().NotBeNull();

        var careerResponse = await client.PostAsJsonAsync("/api/admin/careers",
            new CreateCareerRequest(category!.Id, "Software Engineer", $"software-engineer-{suffix}",
                "Build software", "Detailed", "Create reliable systems", 24));
        careerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var career = await careerResponse.Content.ReadFromJsonAsync<CareerDetailDto>(JsonOptions); career.Should().NotBeNull();

        (await client.PostAsync($"/api/admin/careers/{career!.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.PostAsync($"/api/admin/career-categories/{category.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsync($"/api/admin/careers/{career.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var skillResponse = await client.PostAsJsonAsync($"/api/admin/careers/{career.Id}/skills",
            new AssignCareerSkillRequest(skill.Id, SkillProficiencyLevel.Intermediate, true, 0));
        skillResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var pathwayResponse = await client.PostAsJsonAsync($"/api/admin/careers/{career.Id}/pathways",
            new CreateCareerPathwayRequest(career.Id, "Core pathway", "Main route", "1.0", true));
        pathwayResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var pathway = await pathwayResponse.Content.ReadFromJsonAsync<CareerPathwayDto>(JsonOptions); pathway.Should().NotBeNull();

        var levelResponse = await client.PostAsJsonAsync($"/api/admin/careers/{career.Id}/pathways/{pathway!.Id}/levels",
            new AddPathwayLevelRequest("Foundation", "Start here", 0));
        levelResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var level = await levelResponse.Content.ReadFromJsonAsync<PathwayLevelDto>(JsonOptions); level.Should().NotBeNull();
        (await client.PostAsJsonAsync($"/api/admin/careers/{career.Id}/pathways/{pathway.Id}/levels/{level!.Id}/courses",
            new AddPathwayLevelCourseRequest(course.Id, 0, true))).StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsync($"/api/admin/careers/{career.Id}/pathways/{pathway.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var publicList = await client.GetFromJsonAsync<PagedResult<CareerListItemDto>>("/api/careers", JsonOptions);
        publicList!.Items.Should().ContainSingle(x => x.Id == career.Id);
        var publicCareer = await client.GetFromJsonAsync<CareerDetailDto>($"/api/careers/{career.Slug}", JsonOptions);
        publicCareer!.Skills.Should().ContainSingle(x => x.SkillId == skill.Id);
        publicCareer.PrimaryPathway!.Levels.Single().Courses.Single().CourseId.Should().Be(course.Id);
        var publicPathway = await client.GetFromJsonAsync<CareerPathwayDto>($"/api/careers/{career.Id}/pathway", JsonOptions);
        publicPathway!.Levels.Select(x => x.Order).Should().BeInAscendingOrder();
        publicPathway.Levels.Single().Courses.Select(x => x.Order).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task DraftsRemainHidden_AfterAuthenticatedAdminCreation()
    {
        var suffix = Guid.NewGuid().ToString("N"); var authenticated = await TestApiFactory.CreateAdministratorClientAsync(database);
        using var factory = authenticated.Factory; using var client = authenticated.Client;
        var response = await client.PostAsJsonAsync("/api/admin/career-categories", new CreateCareerCategoryRequest("Draft", $"draft-{suffix}", null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var categories = await client.GetFromJsonAsync<IReadOnlyCollection<CareerCategoryDto>>("/api/career-categories", JsonOptions);
        categories.Should().NotContain(x => x.Slug == $"draft-{suffix}");
    }

    [Fact]
    public async Task DuplicateInvalidAndMissingRequests_ReturnSafeProblemDetails()
    {
        var suffix = Guid.NewGuid().ToString("N"); var authenticated = await TestApiFactory.CreateAdministratorClientAsync(database);
        using var factory = authenticated.Factory; using var client = authenticated.Client;
        var request = new CreateCareerCategoryRequest("Technology", $"duplicate-{suffix}", null);
        (await client.PostAsJsonAsync("/api/admin/career-categories", request)).StatusCode.Should().Be(HttpStatusCode.Created);
        var duplicate = await client.PostAsJsonAsync("/api/admin/career-categories", request);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await duplicate.Content.ReadAsStringAsync()).Should().NotContain("PostgresException");

        var invalid = await client.PostAsJsonAsync("/api/admin/career-categories", new CreateCareerCategoryRequest("", "", null));
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var invalidBody = await invalid.Content.ReadAsStringAsync(); invalidBody.Should().Contain("errors").And.Contain("traceId");

        var missing = await client.GetAsync($"/api/admin/careers/{Guid.NewGuid()}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await missing.Content.ReadAsStringAsync()).Should().Contain("traceId");
    }

    [Fact]
    public async Task HealthAndSwaggerRemainAvailableWithDatabaseConfigured()
    {
        using var factory = TestApiFactory.Create(database); using var client = factory.CreateClient();
        (await client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync("/swagger/index.html")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<(Skill Skill, Course Course)> CreatePublishedDependenciesAsync(string suffix)
    {
        var skill = new Skill("C#", $"csharp-{suffix}", SkillCategory.Technical); skill.Publish();
        var course = new Course("C# Foundations", $"csharp-foundations-{suffix}", "Learn C#.", CourseDifficulty.Foundation, 60);
        course.AddLesson(new Lesson(course.Id, "Introduction", "introduction", LessonContentType.Article, 10, 0)); course.Publish();
        await using var context = database.CreateContext(); context.AddRange(skill, course); await context.SaveChangesAsync();
        return (skill, course);
    }
}
