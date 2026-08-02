using Careersity.Application.Abstractions.Authentication;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.LearningProgress.Dtos;
using Careersity.Application.LearningProgress.Requests;
using Careersity.Application.LearningProgress.Services;
using Careersity.Domain.Careers;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Application;

public sealed class LearningProgressServiceTests
{
    [Fact]
    public async Task CompleteRequiredLessonsUnlocksLaterLevelAndCompletesEnrollment()
    {
        await using var db = TestCatalogContext.Create(); var setup = await CreatePathwayAsync(db);
        var service = new LearningProgressService(db, new TestCurrentUser(setup.UserId));
        var enrollment = await service.EnrollAsync(new(setup.Career.Id), default);
        enrollment.Levels.ElementAt(0).Courses.Single().AvailabilityStatus.Should().Be(CourseAvailabilityStatus.Available);
        enrollment.Levels.ElementAt(1).Courses.Single().AvailabilityStatus.Should().Be(CourseAvailabilityStatus.Locked);
        await service.StartCourseAsync(enrollment.Id, setup.First.Id, default);
        await service.StartCourseAsync(enrollment.Id, setup.First.Id, default);
        db.CourseProgressRecords.Count().Should().Be(1);
        await service.CompleteLessonAsync(enrollment.Id, setup.First.Id, setup.First.Lessons.Single(x => x.IsRequired).Id, default);
        var detail = await service.GetAsync(enrollment.Id, default);
        detail.Levels.ElementAt(0).Courses.Single().IsCompleted.Should().BeTrue();
        detail.Levels.ElementAt(1).Courses.Single().AvailabilityStatus.Should().Be(CourseAvailabilityStatus.Available);
        await service.CompleteLessonAsync(enrollment.Id, setup.Second.Id, setup.Second.Lessons.Single().Id, default);
        detail = await service.GetAsync(enrollment.Id, default);
        detail.Status.Should().Be(EnrollmentStatus.Completed); detail.OverallProgressPercentage.Should().Be(100);
    }

    [Fact]
    public async Task PauseOwnershipDuplicateAndReenrollmentRulesAreEnforced()
    {
        await using var db = TestCatalogContext.Create(); var setup = await CreatePathwayAsync(db);
        var service = new LearningProgressService(db, new TestCurrentUser(setup.UserId));
        var enrollment = await service.EnrollAsync(new(setup.Career.Id), default);
        await FluentActions.Awaiting(() => service.EnrollAsync(new(setup.Career.Id), default)).Should().ThrowAsync<ConflictException>();
        await service.PauseAsync(enrollment.Id, default);
        await FluentActions.Awaiting(() => service.StartCourseAsync(enrollment.Id, setup.First.Id, default)).Should().ThrowAsync<Careersity.Domain.Exceptions.DomainException>();
        await service.ResumeAsync(enrollment.Id, default); await service.WithdrawAsync(enrollment.Id, default);
        var replacement = await service.EnrollAsync(new(setup.Career.Id), default); replacement.Id.Should().NotBe(enrollment.Id);
        var stranger = new LearningProgressService(db, new TestCurrentUser(Guid.NewGuid()));
        await FluentActions.Awaiting(() => stranger.GetAsync(replacement.Id, default)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ExplicitCompletionRejectsMissingRequiredLessonsAndOptionalContentDoesNotReduceProgress()
    {
        await using var db = TestCatalogContext.Create(); var setup = await CreatePathwayAsync(db);
        var service = new LearningProgressService(db, new TestCurrentUser(setup.UserId)); var enrollment = await service.EnrollAsync(new(setup.Career.Id), default);
        await FluentActions.Awaiting(() => service.CompleteCourseAsync(enrollment.Id, setup.First.Id, default)).Should().ThrowAsync<ConflictException>();
        await service.CompleteLessonAsync(enrollment.Id, setup.First.Id, setup.First.Lessons.Single(x => x.IsRequired).Id, default);
        var course = await service.GetCourseAsync(enrollment.Id, setup.First.Id, default);
        course.ProgressPercentage.Should().Be(100); course.IsCompleted.Should().BeTrue();
    }

    private static async Task<(Guid UserId, Career Career, Course First, Course Second)> CreatePathwayAsync(TestCatalogContext db)
    {
        var category = new CareerCategory("Category", $"category-{Guid.NewGuid():N}"); category.Publish();
        var career = new Career(category.Id, "Career", $"career-{Guid.NewGuid():N}", "Description"); career.Publish();
        var first = new Course("First", $"first-{Guid.NewGuid():N}", "Description", CourseDifficulty.Beginner, 30);
        var required = new Lesson(first.Id, "Required", "required", LessonContentType.Article, 10, 0);
        var optional = new Lesson(first.Id, "Optional", "optional", LessonContentType.Article, 10, 1, isRequired: false);
        first.AddLesson(required); first.AddLesson(optional); first.Publish();
        var second = new Course("Second", $"second-{Guid.NewGuid():N}", "Description", CourseDifficulty.Intermediate, 30);
        var secondLesson = new Lesson(second.Id, "Required", "required", LessonContentType.Article, 10, 0); second.AddLesson(secondLesson); second.Publish();
        var pathway = new CareerPathway(career.Id, "Pathway", "1.0", isPrimary: true);
        var level0 = new PathwayLevel(pathway.Id, "Level 1", 0); var level1 = new PathwayLevel(pathway.Id, "Level 2", 1);
        level0.AddCourse(first.Id, 0, true); level1.AddCourse(second.Id, 0, true); pathway.AddLevel(level0); pathway.AddLevel(level1); pathway.Publish();
        db.AddRange(category, career, first, second, pathway); db.Lessons.AddRange(required, optional, secondLesson);
        await db.SaveChangesAsync(); return (Guid.NewGuid(), career, first, second);
    }

    private sealed class TestCurrentUser(Guid id) : ICurrentUser
    { public Guid? UserId => id; public string? Email => "learner@example.test"; public UserRole? Role => UserRole.Learner; public bool IsAuthenticated => true; }
}
