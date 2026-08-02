using Careersity.Application.Common.Exceptions;
using Careersity.Application.CurriculumActivities.Dtos;
using Careersity.Application.CurriculumActivities.Requests;
using Careersity.Application.CurriculumActivities.Services;
using Careersity.Domain.Assessments;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Careersity.UnitTests.Application;

public sealed class CurriculumActivityServiceTests
{
    [Fact]
    public async Task AssessmentCreationUpdateAndPublicVisibilityRespectLifecycle()
    {
        await using var db = TestCatalogContext.Create();
        var course = await AddCourseAsync(db);
        var service = new AssessmentService(db);

        await FluentActions.Awaiting(() => service.CreateAsync(new(Guid.NewGuid(), "Missing", null, 70, null), default))
            .Should().ThrowAsync<NotFoundException>();
        var created = await service.CreateAsync(new(course.Id, " Fundamentals ", " Description ", 70, 3), default);
        created.Status.Should().Be(ContentStatus.Draft);
        created.Title.Should().Be("Fundamentals");
        (await service.ListPublishedAsync(course.Slug, default)).Should().BeEmpty();

        var updated = await service.UpdateAsync(created.Id, new("Updated", null, 80, 2), default);
        updated.PassingScorePercentage.Should().Be(80);

        await AddValidSingleChoiceAsync(db, created.Id);
        await service.PublishAsync(created.Id, default);
        (await service.ListPublishedAsync(course.Slug, default)).Should().ContainSingle(x => x.QuestionCount == 1 && x.TotalPoints == 5);
        await FluentActions.Awaiting(() => service.UpdateAsync(created.Id, new("No", null, 70, null), default))
            .Should().ThrowAsync<ConflictException>();
        await service.ArchiveAsync(created.Id, default);
        (await service.ListPublishedAsync(course.Slug, default)).Should().BeEmpty();
        await FluentActions.Awaiting(() => service.PublishAsync(created.Id, default)).Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task QuestionAndOptionAuthoringRejectsDuplicatesAndSupportsCompleteReorders()
    {
        await using var db = TestCatalogContext.Create();
        var course = await AddCourseAsync(db);
        var assessment = new Assessment(course.Id, "Assessment", 70); db.Assessments.Add(assessment); await db.SaveChangesAsync();
        var questions = new QuestionService(db); var options = new AnswerOptionService(db);
        var first = await questions.AddAsync(assessment.Id, new("First", QuestionType.SingleChoice, 0, 2), default);
        var second = await questions.AddAsync(assessment.Id, new("Second", QuestionType.MultipleChoice, 1, 3), default);
        await FluentActions.Awaiting(() => questions.AddAsync(assessment.Id, new("Duplicate", QuestionType.TrueFalse, 1, 1), default)).Should().ThrowAsync<ConflictException>();

        var one = await options.AddAsync(assessment.Id, first.Id, new("Alpha", true, 0), default);
        var two = await options.AddAsync(assessment.Id, first.Id, new("Beta", false, 1), default);
        await FluentActions.Awaiting(() => options.AddAsync(assessment.Id, first.Id, new(" alpha ", false, 2), default)).Should().ThrowAsync<ConflictException>();
        await FluentActions.Awaiting(() => options.AddAsync(assessment.Id, first.Id, new("Gamma", false, 1), default)).Should().ThrowAsync<ConflictException>();

        await questions.ReorderAsync(assessment.Id, new([new(first.Id, 1), new(second.Id, 0)]), default);
        (await db.Questions.OrderBy(x => x.Order).Select(x => x.Id).ToListAsync()).Should().Equal(second.Id, first.Id);
        await options.ReorderAsync(assessment.Id, first.Id, new([new(one.Id, 1), new(two.Id, 0)]), default);
        (await db.AnswerOptions.Where(x => x.QuestionId == first.Id).OrderBy(x => x.Order).Select(x => x.Id).ToListAsync()).Should().Equal(two.Id, one.Id);

        await FluentActions.Awaiting(() => questions.ReorderAsync(assessment.Id, new([new(first.Id, 0)]), default)).Should().ThrowAsync<ConflictException>();
        await FluentActions.Awaiting(() => options.ReorderAsync(assessment.Id, first.Id, new([new(one.Id, 0), new(two.Id, 0)]), default)).Should().ThrowAsync<ConflictException>();
        await FluentActions.Awaiting(() => options.UpdateAsync(assessment.Id, second.Id, one.Id, new("No", false, 0), default)).Should().ThrowAsync<NotFoundException>();

        var changed = await questions.UpdateAsync(assessment.Id, first.Id, new("Changed type", QuestionType.TrueFalse, 1, 4), default);
        changed.QuestionType.Should().Be(QuestionType.TrueFalse); // Draft questions may temporarily be invalid.
        await options.RemoveAsync(assessment.Id, first.Id, one.Id, default);
        await questions.RemoveAsync(assessment.Id, second.Id, default);
    }

    [Fact]
    public async Task AssessmentPublicationRejectsDraftCourseAndInvalidQuestionShapes()
    {
        await using var db = TestCatalogContext.Create();
        var draftCourse = await AddCourseAsync(db, published: false);
        var assessment = new Assessment(draftCourse.Id, "Assessment", 70); db.Assessments.Add(assessment); await db.SaveChangesAsync();
        var questions = new QuestionService(db); var options = new AnswerOptionService(db); var service = new AssessmentService(db);
        var question = await questions.AddAsync(assessment.Id, new("Choose", QuestionType.SingleChoice, 0, 1), default);
        await options.AddAsync(assessment.Id, question.Id, new("A", true, 0), default);
        await options.AddAsync(assessment.Id, question.Id, new("B", false, 1), default);
        await FluentActions.Awaiting(() => service.PublishAsync(assessment.Id, default)).Should().ThrowAsync<ConflictException>();

        var lesson = new Lesson(draftCourse.Id, "Lesson", "lesson", LessonContentType.Article, 10, 0);
        draftCourse.AddLesson(lesson); db.Lessons.Add(lesson); draftCourse.Publish(); await db.SaveChangesAsync();
        await options.UpdateAsync(assessment.Id, question.Id, (await db.AnswerOptions.SingleAsync(x => !x.IsCorrect)).Id, new("B", true, 1), default);
        await FluentActions.Awaiting(() => service.PublishAsync(assessment.Id, default)).Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task ProjectLifecycleRequiresPublishedCourseAndHidesDraftOrArchivedProjects()
    {
        await using var db = TestCatalogContext.Create();
        var draftCourse = await AddCourseAsync(db, published: false);
        var service = new ProjectService(db);
        await FluentActions.Awaiting(() => service.CreateAsync(ProjectRequest(Guid.NewGuid()), default)).Should().ThrowAsync<NotFoundException>();
        var project = await service.CreateAsync(ProjectRequest(draftCourse.Id), default);
        project.Status.Should().Be(ContentStatus.Draft);
        await FluentActions.Awaiting(() => service.PublishAsync(project.Id, default)).Should().ThrowAsync<ConflictException>();
        var updated = await service.UpdateAsync(project.Id, new("Updated", "Description", "Long instructions", "Output", "Criteria", ProjectSubmissionType.RepositoryUrl, 90), default);
        updated.Instructions.Should().Be("Long instructions");

        var lesson = new Lesson(draftCourse.Id, "Lesson", "lesson", LessonContentType.Article, 10, 0);
        draftCourse.AddLesson(lesson); db.Lessons.Add(lesson); draftCourse.Publish(); await db.SaveChangesAsync();
        await service.PublishAsync(project.Id, default);
        (await service.ListPublishedAsync(draftCourse.Slug, default)).Should().ContainSingle();
        (await service.GetPublishedAsync(draftCourse.Slug, project.Id, default)).Instructions.Should().Be("Long instructions");
        await FluentActions.Awaiting(() => service.UpdateAsync(project.Id, new("No", "Description", "Instructions", null, null, ProjectSubmissionType.Mixed, 10), default)).Should().ThrowAsync<ConflictException>();
        await service.ArchiveAsync(project.Id, default);
        (await service.ListPublishedAsync(draftCourse.Slug, default)).Should().BeEmpty();
        await FluentActions.Awaiting(() => service.GetPublishedAsync(draftCourse.Slug, project.Id, default)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void PublicAssessmentDtosCannotExposeCorrectnessOrAnswerKeys()
    {
        var forbidden = new[] { "IsCorrect", "CorrectAnswer", "CorrectAnswerId", "AnswerKey" };
        typeof(PublicAssessmentSummaryDto).GetProperties().Select(x => x.Name).Should().NotIntersectWith(forbidden);
    }

    private static CreateProjectRequest ProjectRequest(Guid courseId) =>
        new(courseId, "Project", "Description", "Instructions", null, null, ProjectSubmissionType.RepositoryUrl, 60);

    private static async Task<Course> AddCourseAsync(TestCatalogContext db, bool published = true)
    {
        var course = new Course("Course", $"course-{Guid.NewGuid():N}", "Description", CourseDifficulty.Beginner, 60);
        if (published) { course.AddLesson(new(course.Id, "Lesson", "lesson", LessonContentType.Article, 10, 0)); course.Publish(); }
        db.Courses.Add(course); await db.SaveChangesAsync(); return course;
    }

    private static async Task AddValidSingleChoiceAsync(TestCatalogContext db, Guid assessmentId)
    {
        var questions = new QuestionService(db); var options = new AnswerOptionService(db);
        var question = await questions.AddAsync(assessmentId, new("Choose", QuestionType.SingleChoice, 0, 5), default);
        await options.AddAsync(assessmentId, question.Id, new("Right", true, 0), default);
        await options.AddAsync(assessmentId, question.Id, new("Wrong", false, 1), default);
    }
}
