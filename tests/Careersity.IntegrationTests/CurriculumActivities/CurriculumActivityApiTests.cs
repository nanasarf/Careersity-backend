using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Careersity.Application.CurriculumActivities.Dtos;
using Careersity.Application.CurriculumActivities.Requests;
using Careersity.Application.Identity.Dtos;
using Careersity.Application.Identity.Requests;
using Careersity.Application.LearningContent.Dtos;
using Careersity.Application.LearningContent.Requests;
using Careersity.Domain.Enums;
using Careersity.IntegrationTests.Persistence;
using FluentAssertions;
using Xunit;

namespace Careersity.IntegrationTests.CurriculumActivities;

[Collection(PostgreSqlCollection.Name)]
public sealed class CurriculumActivityApiTests(PostgreSqlFixture database)
{
    [Fact]
    public async Task ActivityAdminRoutesRequireAdministratorWhilePublicRoutesRemainAnonymous()
    {
        using var factory = TestApiFactory.Create(database); using var client = factory.CreateClient();
        (await client.GetAsync("/api/admin/assessments")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/admin/projects")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var email = $"activity-learner-{Guid.NewGuid():N}@example.com";
        var registration = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Test", "Learner", "Valid!Pass1", "Valid!Pass1"));
        var auth = await registration.Content.ReadFromJsonAsync<AuthenticationResultDto>(TestApiFactory.JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        (await client.GetAsync("/api/admin/assessments")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/admin/projects")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var administrator = await TestApiFactory.CreateAdministratorClientAsync(database);
        using var adminFactory = administrator.Factory; using var admin = administrator.Client;
        (await admin.GetAsync("/api/admin/assessments")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.GetAsync("/api/admin/projects")).StatusCode.Should().Be(HttpStatusCode.OK);
        admin.DefaultRequestHeaders.Authorization = null;
        (await admin.GetAsync($"/api/courses/not-found-{Guid.NewGuid():N}/assessments")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteAssessmentFlowPublishesSafeSummaryAndEnforcesImmutability()
    {
        var authenticated = await TestApiFactory.CreateAdministratorClientAsync(database);
        using var factory = authenticated.Factory; using var client = authenticated.Client;
        var administratorAuthorization = client.DefaultRequestHeaders.Authorization;
        var course = await CreatePublishedCourseAsync(client);
        var assessmentResponse = await client.PostAsJsonAsync("/api/admin/assessments",
            new CreateAssessmentRequest(course.Id, "Knowledge Check", "Checks fundamentals", 70, 3));
        assessmentResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var assessment = (await assessmentResponse.Content.ReadFromJsonAsync<AssessmentAdminDetailDto>(TestApiFactory.JsonOptions))!;

        var first = await AddQuestionAsync(client, assessment.Id, "Choose one", QuestionType.SingleChoice, 0, 2);
        var second = await AddQuestionAsync(client, assessment.Id, "Choose many", QuestionType.MultipleChoice, 1, 3);
        var third = await AddQuestionAsync(client, assessment.Id, "True or false", QuestionType.TrueFalse, 2, 1);
        var firstOptions = await AddOptionsAsync(client, assessment.Id, first.Id, [("Right", true), ("Wrong", false)]);
        await AddOptionsAsync(client, assessment.Id, second.Id, [("One", true), ("Two", true), ("Three", false)]);
        await AddOptionsAsync(client, assessment.Id, third.Id, [("True", true), ("False", false)]);

        (await client.PutAsJsonAsync($"/api/admin/assessments/{assessment.Id}/questions/reorder",
            new ReorderQuestionsRequest([new(first.Id, 2), new(second.Id, 0), new(third.Id, 1)]))).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PutAsJsonAsync($"/api/admin/assessments/{assessment.Id}/questions/{first.Id}/answer-options/reorder",
            new ReorderAnswerOptionsRequest([new(firstOptions[0].Id, 1), new(firstOptions[1].Id, 0)]))).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.PostAsync($"/api/admin/assessments/{assessment.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        client.DefaultRequestHeaders.Authorization = null;
        var response = await client.GetAsync($"/api/courses/{course.Slug}/assessments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("isCorrect").And.NotContain("correctAnswer").And.NotContain("answerKey").And.NotContain("passwordHash").And.NotContain("tokenHash");
        (await response.Content.ReadFromJsonAsync<IReadOnlyCollection<PublicAssessmentSummaryDto>>(TestApiFactory.JsonOptions))!
            .Should().ContainSingle(x => x.Id == assessment.Id && x.QuestionCount == 3 && x.TotalPoints == 6);

        client.DefaultRequestHeaders.Authorization = administratorAuthorization;
        (await client.PutAsJsonAsync($"/api/admin/assessments/{assessment.Id}", new UpdateAssessmentRequest("No", null, 80, null))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.PostAsync($"/api/admin/assessments/{assessment.Id}/archive", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        client.DefaultRequestHeaders.Authorization = null;
        (await client.GetFromJsonAsync<IReadOnlyCollection<PublicAssessmentSummaryDto>>($"/api/courses/{course.Slug}/assessments", TestApiFactory.JsonOptions))!.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteProjectFlowPublishesInstructionsAndHidesArchivedProject()
    {
        var authenticated = await TestApiFactory.CreateAdministratorClientAsync(database);
        using var factory = authenticated.Factory; using var client = authenticated.Client;
        var administratorAuthorization = client.DefaultRequestHeaders.Authorization;
        var course = await CreatePublishedCourseAsync(client);
        var create = await client.PostAsJsonAsync("/api/admin/projects", new CreateProjectRequest(course.Id, "Portfolio Project", "Build a portfolio",
            "Create and document the solution.", "A repository URL", "Correctness and clarity", ProjectSubmissionType.RepositoryUrl, 180));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var project = (await create.Content.ReadFromJsonAsync<ProjectAdminDetailDto>(TestApiFactory.JsonOptions))!;
        (await client.PutAsJsonAsync($"/api/admin/projects/{project.Id}", new UpdateProjectRequest(project.Title, project.Description,
            "Updated full instructions", project.ExpectedOutput, project.EvaluationCriteria, project.SubmissionType, 200))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.PostAsync($"/api/admin/projects/{project.Id}/publish", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        client.DefaultRequestHeaders.Authorization = null;
        (await client.GetFromJsonAsync<IReadOnlyCollection<PublicProjectSummaryDto>>($"/api/courses/{course.Slug}/projects", TestApiFactory.JsonOptions))!.Should().ContainSingle(x => x.Id == project.Id);
        var detail = await client.GetFromJsonAsync<PublicProjectDetailDto>($"/api/courses/{course.Slug}/projects/{project.Id}", TestApiFactory.JsonOptions);
        detail!.Instructions.Should().Be("Updated full instructions");

        client.DefaultRequestHeaders.Authorization = administratorAuthorization;
        (await client.PutAsJsonAsync($"/api/admin/projects/{project.Id}", new UpdateProjectRequest("No", "Description", "Instructions", null, null, ProjectSubmissionType.Mixed, 10))).StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await client.PostAsync($"/api/admin/projects/{project.Id}/archive", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        client.DefaultRequestHeaders.Authorization = null;
        (await client.GetAsync($"/api/courses/{course.Slug}/projects/{project.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssessmentAuthoringErrorsReturnSafeProblemDetails()
    {
        var authenticated = await TestApiFactory.CreateAdministratorClientAsync(database);
        using var factory = authenticated.Factory; using var client = authenticated.Client;
        var course = await CreateCourseAsync(client, $"draft-{Guid.NewGuid():N}");
        var missing = await client.PostAsJsonAsync("/api/admin/assessments", new CreateAssessmentRequest(Guid.NewGuid(), "Missing", null, 70, null));
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var assessmentResponse = await client.PostAsJsonAsync("/api/admin/assessments", new CreateAssessmentRequest(course.Id, "Draft Assessment", null, 70, null));
        var assessment = (await assessmentResponse.Content.ReadFromJsonAsync<AssessmentAdminDetailDto>(TestApiFactory.JsonOptions))!;
        var question = await AddQuestionAsync(client, assessment.Id, "Question", QuestionType.SingleChoice, 0, 1);
        var duplicateQuestion = await client.PostAsJsonAsync($"/api/admin/assessments/{assessment.Id}/questions", new AddQuestionRequest("Duplicate", QuestionType.TrueFalse, 0, 1));
        duplicateQuestion.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await AddOptionsAsync(client, assessment.Id, question.Id, [("Same", true)]);
        var duplicateText = await client.PostAsJsonAsync($"/api/admin/assessments/{assessment.Id}/questions/{question.Id}/answer-options", new AddAnswerOptionRequest(" same ", false, 1));
        duplicateText.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var publish = await client.PostAsync($"/api/admin/assessments/{assessment.Id}/publish", null);
        publish.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await publish.Content.ReadAsStringAsync()).Should().Contain("traceId").And.NotContain("PostgresException");
    }

    private static async Task<CourseDetailDto> CreatePublishedCourseAsync(HttpClient client)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var skillResponse = await client.PostAsJsonAsync("/api/admin/skills", new CreateSkillRequest("Activity Skill", $"activity-skill-{suffix}", null, SkillCategory.Technical));
        var skill = (await skillResponse.Content.ReadFromJsonAsync<SkillDto>(TestApiFactory.JsonOptions))!;
        await client.PostAsync($"/api/admin/skills/{skill.Id}/publish", null);
        var course = await CreateCourseAsync(client, $"activity-course-{suffix}");
        await client.PostAsJsonAsync($"/api/admin/courses/{course.Id}/lessons", new AddLessonRequest("Required Lesson", "required-lesson", null, "Content", LessonContentType.Article, null, 10, 0, true));
        await client.PostAsJsonAsync($"/api/admin/courses/{course.Id}/skills", new AddCourseSkillRequest(skill.Id, SkillProficiencyLevel.Beginner, true));
        (await client.PostAsync($"/api/admin/courses/{course.Id}/publish", null)).EnsureSuccessStatusCode();
        return course;
    }

    private static async Task<CourseDetailDto> CreateCourseAsync(HttpClient client, string slug)
    {
        var response = await client.PostAsJsonAsync("/api/admin/courses", new CreateCourseRequest("Activity Course", slug, "Description", null, CourseDifficulty.Beginner, 60));
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<CourseDetailDto>(TestApiFactory.JsonOptions))!;
    }

    private static async Task<QuestionAdminDto> AddQuestionAsync(HttpClient client, Guid assessmentId, string prompt, QuestionType type, int order, int points)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/assessments/{assessmentId}/questions", new AddQuestionRequest(prompt, type, order, points));
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<QuestionAdminDto>(TestApiFactory.JsonOptions))!;
    }

    private static async Task<List<AnswerOptionAdminDto>> AddOptionsAsync(HttpClient client, Guid assessmentId, Guid questionId, IReadOnlyList<(string Text, bool Correct)> values)
    {
        var result = new List<AnswerOptionAdminDto>();
        for (var order = 0; order < values.Count; order++)
        {
            var response = await client.PostAsJsonAsync($"/api/admin/assessments/{assessmentId}/questions/{questionId}/answer-options", new AddAnswerOptionRequest(values[order].Text, values[order].Correct, order));
            response.EnsureSuccessStatusCode(); result.Add((await response.Content.ReadFromJsonAsync<AnswerOptionAdminDto>(TestApiFactory.JsonOptions))!);
        }
        return result;
    }
}
