using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Careersity.Application.Common.Models;
using Careersity.Application.Identity.Dtos;
using Careersity.Application.Identity.Requests;
using Careersity.Application.LearningContent.Dtos;
using Careersity.Application.LearningContent.Requests;
using Careersity.Domain.Enums;
using Careersity.IntegrationTests.Persistence;
using FluentAssertions;
using Xunit;

namespace Careersity.IntegrationTests.LearningContent;

[Collection(PostgreSqlCollection.Name)]
public sealed class LearningContentApiTests(PostgreSqlFixture database)
{
    [Fact]
    public async Task LearningContentAdminRoutesEnforceAdministratorPolicy()
    {
        using var factory = TestApiFactory.Create(database); using var client = factory.CreateClient();
        (await client.GetAsync("/api/admin/skills")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/admin/courses")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var email = $"learner-content-{Guid.NewGuid():N}@example.com";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Test", "Learner", "Valid!Pass1", "Valid!Pass1"));
        var auth = await registration.Content.ReadFromJsonAsync<AuthenticationResultDto>(TestApiFactory.JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        (await client.GetAsync("/api/admin/skills")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/admin/courses")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var administrator = await TestApiFactory.CreateAdministratorClientAsync(database);
        using var adminFactory = administrator.Factory; using var admin = administrator.Client;
        (await admin.GetAsync("/api/admin/skills")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.GetAsync("/api/admin/courses")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CompleteSkillFlowPublishesAndArchivesWithoutPublicLeakage()
    {
        var authenticated = await TestApiFactory.CreateAdministratorClientAsync(database);
        using var factory = authenticated.Factory; using var client = authenticated.Client;
        var suffix = Guid.NewGuid().ToString("N");
        var created = await CreateSkillAsync(client, $"skill-{suffix}");
        created.Status.Should().Be(ContentStatus.Draft);
        var update = await client.PutAsJsonAsync($"/api/admin/skills/{created.Id}",
            new UpdateSkillRequest("Updated Skill", created.Slug, "Updated description", SkillCategory.Analytical));
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync($"/api/admin/skills/{created.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var publicSkill = await client.GetFromJsonAsync<SkillDto>($"/api/skills/{created.Slug}", TestApiFactory.JsonOptions);
        publicSkill!.Status.Should().Be(ContentStatus.Published);
        var duplicate = await client.PostAsJsonAsync("/api/admin/skills", new CreateSkillRequest("Duplicate", created.Slug, null, SkillCategory.Tool));
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await duplicate.Content.ReadAsStringAsync()).Should().NotContain("PostgresException");
        (await client.PutAsJsonAsync($"/api/admin/skills/{created.Id}", new UpdateSkillRequest("No", "no", null, SkillCategory.Tool))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.PostAsync($"/api/admin/skills/{created.Id}/archive", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/skills/{created.Slug}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteCourseFlowPersistsOrdersRelationshipsPublicationAndPublicContent()
    {
        var authenticated = await TestApiFactory.CreateAdministratorClientAsync(database);
        using var factory = authenticated.Factory; using var client = authenticated.Client;
        var suffix = Guid.NewGuid().ToString("N"); var skill = await CreateSkillAsync(client, $"course-skill-{suffix}");
        await client.PostAsync($"/api/admin/skills/{skill.Id}/publish", null);

        var prerequisite = await CreateCourseAsync(client, "Prerequisite", $"prerequisite-{suffix}");
        await AddLessonAsync(client, prerequisite.Id, "Prerequisite Lesson", "prerequisite-lesson", 0, "Prerequisite body");
        await AddSkillAsync(client, prerequisite.Id, skill.Id);
        (await client.PostAsync($"/api/admin/courses/{prerequisite.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var target = await CreateCourseAsync(client, "Target Course", $"target-{suffix}");
        var first = await AddLessonAsync(client, target.Id, "First", "first", 0, "First body");
        var second = await AddLessonAsync(client, target.Id, "Second", "second", 1, "Second body");
        var reorder = await client.PutAsJsonAsync($"/api/admin/courses/{target.Id}/lessons/reorder",
            new ReorderLessonsRequest([new(first.Id, 1), new(second.Id, 0)]));
        reorder.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await AddSkillAsync(client, target.Id, skill.Id);
        var prerequisiteResponse = await client.PostAsJsonAsync($"/api/admin/courses/{target.Id}/prerequisites", new AddCoursePrerequisiteRequest(prerequisite.Id, true));
        prerequisiteResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.PostAsync($"/api/admin/courses/{target.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detail = await client.GetFromJsonAsync<CourseDetailDto>($"/api/courses/{target.Slug}", TestApiFactory.JsonOptions);
        detail!.Lessons.Select(x => x.Id).Should().Equal(second.Id, first.Id);
        detail.Lessons.Should().OnlyContain(x => x.IsRequired);
        detail.Prerequisites.Should().ContainSingle(x => x.PrerequisiteCourseId == prerequisite.Id);
        detail.Skills.Should().ContainSingle(x => x.SkillId == skill.Id && x.IsPrimary);
        var lesson = await client.GetFromJsonAsync<LessonDto>($"/api/courses/{target.Slug}/lessons/{first.Slug}", TestApiFactory.JsonOptions);
        lesson!.Content.Should().Be("First body");
        (await client.PutAsJsonAsync($"/api/admin/courses/{target.Id}", CourseUpdate(target))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.PostAsync($"/api/admin/courses/{target.Id}/archive", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/courses/{target.Slug}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await client.GetAsync($"/api/courses/{target.Slug}/lessons/{first.Slug}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CourseErrorsReturnSafeConflictsIncludingCycleAndPublicationDependencies()
    {
        var authenticated = await TestApiFactory.CreateAdministratorClientAsync(database);
        using var factory = authenticated.Factory; using var client = authenticated.Client; var suffix = Guid.NewGuid().ToString("N");
        var a = await CreateCourseAsync(client, "A", $"a-{suffix}"); var b = await CreateCourseAsync(client, "B", $"b-{suffix}");
        (await client.PostAsJsonAsync($"/api/admin/courses/{a.Id}/prerequisites", new AddCoursePrerequisiteRequest(b.Id, true))).StatusCode.Should().Be(HttpStatusCode.Created);
        var cycle = await client.PostAsJsonAsync($"/api/admin/courses/{b.Id}/prerequisites", new AddCoursePrerequisiteRequest(a.Id, true));
        cycle.StatusCode.Should().Be(HttpStatusCode.Conflict); (await cycle.Content.ReadAsStringAsync()).Should().Contain("circular").And.Contain("traceId");
        await AddLessonAsync(client, a.Id, "One", "one", 0, null);
        var duplicateOrder = await client.PostAsJsonAsync($"/api/admin/courses/{a.Id}/lessons", Lesson("Two", "two", 0, null));
        duplicateOrder.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var duplicateSlug = await client.PostAsJsonAsync($"/api/admin/courses/{a.Id}/lessons", Lesson("Two", "one", 1, null));
        duplicateSlug.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var invalidUrl = await client.PostAsJsonAsync($"/api/admin/courses/{a.Id}/lessons",
            new AddLessonRequest("External", "external", null, null, LessonContentType.ExternalResource, "relative", 10, 1, true));
        invalidUrl.StatusCode.Should().Be(HttpStatusCode.BadRequest); (await invalidUrl.Content.ReadAsStringAsync()).Should().Contain("traceId");
        var unpublishedSkill = await CreateSkillAsync(client, $"draft-skill-{suffix}"); await AddSkillAsync(client, a.Id, unpublishedSkill.Id);
        var publish = await client.PostAsync($"/api/admin/courses/{a.Id}/publish", null); publish.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await publish.Content.ReadAsStringAsync()).Should().Contain("traceId");
    }

    private static async Task<SkillDto> CreateSkillAsync(HttpClient client, string slug)
    { var response = await client.PostAsJsonAsync("/api/admin/skills", new CreateSkillRequest("Skill", slug, "Description", SkillCategory.Technical)); response.StatusCode.Should().Be(HttpStatusCode.Created); return (await response.Content.ReadFromJsonAsync<SkillDto>(TestApiFactory.JsonOptions))!; }
    private static async Task<CourseDetailDto> CreateCourseAsync(HttpClient client, string title, string slug)
    { var response = await client.PostAsJsonAsync("/api/admin/courses", new CreateCourseRequest(title, slug, "Description", "Details", CourseDifficulty.Beginner, 60)); response.StatusCode.Should().Be(HttpStatusCode.Created); return (await response.Content.ReadFromJsonAsync<CourseDetailDto>(TestApiFactory.JsonOptions))!; }
    private static async Task<LessonDto> AddLessonAsync(HttpClient client, Guid courseId, string title, string slug, int order, string? content)
    { var response = await client.PostAsJsonAsync($"/api/admin/courses/{courseId}/lessons", Lesson(title, slug, order, content)); response.StatusCode.Should().Be(HttpStatusCode.Created); return (await response.Content.ReadFromJsonAsync<LessonDto>(TestApiFactory.JsonOptions))!; }
    private static AddLessonRequest Lesson(string title, string slug, int order, string? content) => new(title, slug, "Summary", content, LessonContentType.Article, null, 10, order, true);
    private static async Task AddSkillAsync(HttpClient client, Guid courseId, Guid skillId) =>
        (await client.PostAsJsonAsync($"/api/admin/courses/{courseId}/skills", new AddCourseSkillRequest(skillId, SkillProficiencyLevel.Beginner, true))).StatusCode.Should().Be(HttpStatusCode.Created);
    private static UpdateCourseRequest CourseUpdate(CourseDetailDto course) => new(course.Title, course.Slug, course.ShortDescription, course.DetailedDescription, course.Difficulty, course.EstimatedDurationMinutes);
}
