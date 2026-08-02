using Careersity.Application.Common.Exceptions;
using Careersity.Domain.Assessments;
using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using Careersity.Domain.Identity;
using Careersity.Domain.Learning;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Careersity.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class AssessmentAttemptPersistenceTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task AggregatePersistsResponsesOptionsGradeAndReadableStatus()
    {
        var data = CreateGraph(); var attempt = new AssessmentAttempt(data.User.Id, data.Enrollment.Id, data.Progress.Id, data.Assessment.Id, 1);
        attempt.AddOrUpdateResponse(data.Question.Id, [data.Correct.Id]);
        attempt.Submit(100, data.Question.Points, data.Question.Points, true, new Dictionary<Guid, (bool, int)> { [data.Question.Id] = (true, data.Question.Points) });
        await using (var db = fixture.CreateContext()) { db.AddRange(data.User, data.Category, data.Career, data.Pathway, data.Course, data.Enrollment, data.Assessment, attempt); await db.SaveChangesAsync(); }

        await using var verify = fixture.CreateContext();
        var loaded = await verify.AssessmentAttempts.Include(x => x.Responses).ThenInclude(x => x.SelectedOptions).SingleAsync(x => x.Id == attempt.Id);
        loaded.Status.Should().Be(AssessmentAttemptStatus.Passed); loaded.Responses.Should().ContainSingle();
        loaded.Responses.Single().SelectedOptions.Should().ContainSingle(x => x.AnswerOptionId == data.Correct.Id);
        var status = await verify.Database.SqlQuery<string>($"SELECT \"Status\" AS \"Value\" FROM \"AssessmentAttempts\" WHERE \"Id\" = {attempt.Id}").SingleAsync();
        status.Should().Be("Passed");
    }

    [Fact]
    public async Task UniqueAttemptNumberAndSingleActiveAttemptAreDatabaseProtected()
    {
        var data = CreateGraph();
        await using (var seed = fixture.CreateContext()) { seed.AddRange(data.User, data.Category, data.Career, data.Pathway, data.Course, data.Enrollment, data.Assessment); seed.AssessmentAttempts.Add(new(data.User.Id, data.Enrollment.Id, data.Progress.Id, data.Assessment.Id, 1)); await seed.SaveChangesAsync(); }
        await using var duplicate = fixture.CreateContext();
        duplicate.AssessmentAttempts.Add(new(data.User.Id, data.Enrollment.Id, data.Progress.Id, data.Assessment.Id, 2));
        await duplicate.Invoking(x => x.SaveChangesAsync()).Should().ThrowAsync<ConflictException>();
    }

    private static Graph CreateGraph()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var user = new User($"attempt-{suffix}@example.com", "Test", "Learner", new PasswordHasher<User>().HashPassword(null!, "Valid!Pass1"), UserRole.Learner);
        var category = new CareerCategory("Category", $"attempt-category-{suffix}"); var career = new Career(category.Id, "Career", $"attempt-career-{suffix}", "Description");
        var pathway = new CareerPathway(career.Id, "Path", "1.0"); var course = new Course("Course", $"attempt-course-{suffix}", "Description", CourseDifficulty.Beginner, 30);
        var lesson = new Lesson(course.Id, "Lesson", "lesson", LessonContentType.Article, 10, 0); course.AddLesson(lesson);
        var assessment = new Assessment(course.Id, "Assessment", 70); var question = new Question(assessment.Id, "Question", QuestionType.SingleChoice, 0, 4);
        var correct = new AnswerOption(question.Id, "Correct", true, 0); question.AddAnswerOption(correct); question.AddAnswerOption(new(question.Id, "Wrong", false, 1)); assessment.AddQuestion(question);
        var enrollment = new CareerEnrollment(user.Id, career.Id, pathway.Id); var progress = enrollment.AddCourseProgress(course.Id);
        return new(user, category, career, pathway, course, enrollment, progress, assessment, question, correct);
    }

    private sealed record Graph(User User, CareerCategory Category, Career Career, CareerPathway Pathway, Course Course,
        CareerEnrollment Enrollment, CourseProgress Progress, Assessment Assessment, Question Question, AnswerOption Correct);
}
