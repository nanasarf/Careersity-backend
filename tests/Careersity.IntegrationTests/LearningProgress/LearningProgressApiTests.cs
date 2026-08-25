using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Careersity.Application.Identity.Dtos;
using Careersity.Application.Identity.Requests;
using Careersity.Application.LearningProgress.Dtos;
using Careersity.Application.LearningProgress.Requests;
using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using Careersity.Domain.Skills;
using Careersity.IntegrationTests.Persistence;
using FluentAssertions;
using Xunit;

namespace Careersity.IntegrationTests.LearningProgress;

[Collection(PostgreSqlCollection.Name)]
public sealed class LearningProgressApiTests(PostgreSqlFixture database)
{
    [Fact]
    public async Task CompleteLearnerFlowEnforcesUnlockingLifecycleOwnershipAndAutomaticCompletion()
    {
        var content = await CreatePublishedPathwayAsync();
        using var factory = TestApiFactory.Create(database); using var client = factory.CreateClient();
        (await client.GetAsync("/api/me/career-enrollments")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AuthenticateLearnerAsync(client, $"progress-{Guid.NewGuid():N}@example.com");

        var create = await client.PostAsJsonAsync("/api/me/career-enrollments", new EnrollInCareerRequest(content.CareerId));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var enrollment = (await create.Content.ReadFromJsonAsync<CareerEnrollmentDetailDto>(TestApiFactory.JsonOptions))!;
        enrollment.Levels.ElementAt(0).Courses.Single().AvailabilityStatus.Should().Be(CourseAvailabilityStatus.Available);
        enrollment.Levels.ElementAt(1).Courses.Single().AvailabilityStatus.Should().Be(CourseAvailabilityStatus.Locked);
        (await client.PostAsJsonAsync("/api/me/career-enrollments", new EnrollInCareerRequest(content.CareerId))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.PostAsync($"/api/me/career-enrollments/{enrollment.Id}/courses/{content.SecondCourseId}/start", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await client.PostAsync($"/api/me/career-enrollments/{enrollment.Id}/pause", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsync($"/api/me/career-enrollments/{enrollment.Id}/courses/{content.FirstCourseId}/start", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.PostAsync($"/api/me/career-enrollments/{enrollment.Id}/resume", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsync($"/api/me/career-enrollments/{enrollment.Id}/courses/{content.FirstCourseId}/start", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync($"/api/me/career-enrollments/{enrollment.Id}/courses/{content.FirstCourseId}/start", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync($"/api/me/career-enrollments/{enrollment.Id}/courses/{content.FirstCourseId}/lessons/{content.FirstLessonId}/complete", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync($"/api/me/career-enrollments/{enrollment.Id}/courses/{content.FirstCourseId}/lessons/{content.FirstLessonId}/complete", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        enrollment = (await client.GetFromJsonAsync<CareerEnrollmentDetailDto>($"/api/me/career-enrollments/{enrollment.Id}", TestApiFactory.JsonOptions))!;
        enrollment.OverallProgressPercentage.Should().Be(50);
        enrollment.Levels.ElementAt(1).Courses.Single().AvailabilityStatus.Should().Be(CourseAvailabilityStatus.Available);
        (await client.PostAsync($"/api/me/career-enrollments/{enrollment.Id}/courses/{content.SecondCourseId}/lessons/{content.SecondLessonId}/complete", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        enrollment = (await client.GetFromJsonAsync<CareerEnrollmentDetailDto>($"/api/me/career-enrollments/{enrollment.Id}", TestApiFactory.JsonOptions))!;
        enrollment.Status.Should().Be(EnrollmentStatus.Completed); enrollment.OverallProgressPercentage.Should().Be(100);

        using var stranger = factory.CreateClient(); await AuthenticateLearnerAsync(stranger, $"stranger-{Guid.NewGuid():N}@example.com");
        (await stranger.GetAsync($"/api/me/career-enrollments/{enrollment.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WithdrawalPreservesHistoryAndAllowsFreshEnrollment()
    {
        var content = await CreatePublishedPathwayAsync(); using var factory = TestApiFactory.Create(database); using var client = factory.CreateClient();
        await AuthenticateLearnerAsync(client, $"withdraw-{Guid.NewGuid():N}@example.com");
        var first = (await (await client.PostAsJsonAsync("/api/me/career-enrollments", new EnrollInCareerRequest(content.CareerId))).Content.ReadFromJsonAsync<CareerEnrollmentDetailDto>(TestApiFactory.JsonOptions))!;
        await client.PostAsync($"/api/me/career-enrollments/{first.Id}/withdraw", null);
        (await client.PostAsync($"/api/me/career-enrollments/{first.Id}/courses/{content.FirstCourseId}/start", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        var secondResponse = await client.PostAsJsonAsync("/api/me/career-enrollments", new EnrollInCareerRequest(content.CareerId));
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var active = await client.GetFromJsonAsync<Careersity.Application.Common.Models.PagedResult<CareerEnrollmentListItemDto>>("/api/me/career-enrollments", TestApiFactory.JsonOptions);
        active!.Items.Should().ContainSingle();
        var history = await client.GetFromJsonAsync<Careersity.Application.Common.Models.PagedResult<CareerEnrollmentListItemDto>>("/api/me/career-enrollments?includeHistory=true", TestApiFactory.JsonOptions);
        history!.Items.Should().Contain(x => x.Id == first.Id && x.Status == EnrollmentStatus.Withdrawn);
    }

    [Fact]
    public async Task ParallelCourseStartsCreateOnlyOneProgressRecord()
    {
        var content = await CreatePublishedPathwayAsync(); using var factory = TestApiFactory.Create(database); using var client = factory.CreateClient();
        await AuthenticateLearnerAsync(client, $"concurrent-{Guid.NewGuid():N}@example.com");
        var enrollment = (await (await client.PostAsJsonAsync("/api/me/career-enrollments", new EnrollInCareerRequest(content.CareerId))).Content.ReadFromJsonAsync<CareerEnrollmentDetailDto>(TestApiFactory.JsonOptions))!;
        var authorization = client.DefaultRequestHeaders.Authorization;
        using var first = factory.CreateClient(); using var second = factory.CreateClient(); first.DefaultRequestHeaders.Authorization = authorization; second.DefaultRequestHeaders.Authorization = authorization;
        var responses = await Task.WhenAll(first.PostAsync($"/api/me/career-enrollments/{enrollment.Id}/courses/{content.FirstCourseId}/start", null),
            second.PostAsync($"/api/me/career-enrollments/{enrollment.Id}/courses/{content.FirstCourseId}/start", null));
        responses.Should().Contain(x => x.StatusCode == HttpStatusCode.OK);
        await using var context = database.CreateContext();
        (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(context.CourseProgressRecords, x => x.CareerEnrollmentId == enrollment.Id)).Should().Be(1);
    }

    private async Task<(Guid CareerId, Guid FirstCourseId, Guid FirstLessonId, Guid SecondCourseId, Guid SecondLessonId)> CreatePublishedPathwayAsync()
    {
        var suffix = Guid.NewGuid().ToString("N"); var category = new CareerCategory("Progress", $"progress-{suffix}"); category.Publish();
        var career = new Career(category.Id, "Progress Career", $"progress-career-{suffix}", "Description"); career.Publish();
        var skill = new Skill("Progress Skill", $"progress-skill-{suffix}", SkillCategory.Technical); skill.Publish();
        var careerSkill = new CareerSkill(career.Id, skill.Id, SkillProficiencyLevel.Beginner, true, 0);
        var first = new Course("First", $"progress-first-{suffix}", "Description", CourseDifficulty.Beginner, 30);
        var firstLesson = new Lesson(first.Id, "First Lesson", "first-lesson", LessonContentType.Article, 10, 0); first.AddLesson(firstLesson); first.AssociateSkill(skill.Id, SkillProficiencyLevel.Beginner, true); first.Publish();
        var second = new Course("Second", $"progress-second-{suffix}", "Description", CourseDifficulty.Intermediate, 30);
        var secondLesson = new Lesson(second.Id, "Second Lesson", "second-lesson", LessonContentType.Article, 10, 0); second.AddLesson(secondLesson); second.AssociateSkill(skill.Id, SkillProficiencyLevel.Beginner, true); second.Publish();
        var pathway = new CareerPathway(career.Id, "Primary", "1.0", isPrimary: true); var firstLevel = new PathwayLevel(pathway.Id, "Foundation", 0); var secondLevel = new PathwayLevel(pathway.Id, "Advanced", 1);
        firstLevel.AddCourse(first.Id, 0, true); secondLevel.AddCourse(second.Id, 0, true); pathway.AddLevel(firstLevel); pathway.AddLevel(secondLevel); pathway.Publish();
        await using var db = database.CreateContext(); db.AddRange(category, career, skill, careerSkill, first, second, pathway); await db.SaveChangesAsync();
        return (career.Id, first.Id, firstLesson.Id, second.Id, secondLesson.Id);
    }

    private static async Task AuthenticateLearnerAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Test", "Learner", "Valid!Pass1", "Valid!Pass1"));
        response.EnsureSuccessStatusCode(); var auth = await response.Content.ReadFromJsonAsync<AuthenticationResultDto>(TestApiFactory.JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
    }
}
