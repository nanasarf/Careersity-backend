using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;
using Careersity.Domain.Learning;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Domain.Learning;

public sealed class LearningProgressTests
{
    [Fact]
    public void EnrollmentLifecycleAndDuplicateCourseProgressAreProtected()
    {
        var enrollment = new CareerEnrollment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        var course = enrollment.AddCourseProgress(Guid.NewGuid());
        enrollment.AddCourseProgress(course.CourseId).Should().BeSameAs(course);
        enrollment.CourseProgressRecords.Should().ContainSingle();
        enrollment.Pause(); enrollment.Status.Should().Be(EnrollmentStatus.Paused);
        FluentActions.Invoking(() => enrollment.AddCourseProgress(Guid.NewGuid())).Should().Throw<DomainException>();
        enrollment.Resume(); enrollment.Withdraw();
        FluentActions.Invoking(enrollment.Resume).Should().Throw<DomainException>();
    }

    [Fact]
    public void CompletedEnrollmentCannotTransitionAndCompletionIsIdempotent()
    {
        var enrollment = new CareerEnrollment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        enrollment.Complete(); var completedAt = enrollment.CompletedAtUtc; enrollment.Complete();
        enrollment.CompletedAtUtc.Should().Be(completedAt);
        FluentActions.Invoking(enrollment.Pause).Should().Throw<DomainException>();
        FluentActions.Invoking(enrollment.Withdraw).Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 1, 0)]
    public void EnrollmentRequiresAllReferences(int user, int career, int pathway)
    {
        var action = () => new CareerEnrollment(user == 0 ? Guid.Empty : Guid.NewGuid(), career == 0 ? Guid.Empty : Guid.NewGuid(), pathway == 0 ? Guid.Empty : Guid.NewGuid());
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void LessonProgressStartAndCompletionAreIdempotent()
    {
        var enrollment = new CareerEnrollment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var course = enrollment.AddCourseProgress(Guid.NewGuid()); var lesson = course.AddLessonProgress(Guid.NewGuid());
        course.AddLessonProgress(lesson.LessonId).Should().BeSameAs(lesson);
        lesson.Complete(); var completed = lesson.CompletedAtUtc; lesson.Complete();
        lesson.StartedAtUtc.Should().BeOnOrBefore(lesson.CompletedAtUtc!.Value); lesson.CompletedAtUtc.Should().Be(completed);
        course.Complete(); var courseCompleted = course.CompletedAtUtc; course.Complete(); course.CompletedAtUtc.Should().Be(courseCompleted);
    }
}
