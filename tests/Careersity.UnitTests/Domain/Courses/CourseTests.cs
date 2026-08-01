using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Domain.Courses;

public sealed class CourseTests
{
    [Fact] public void Constructor_CreatesDraft() => TestData.Course().Status.Should().Be(ContentStatus.Draft);

    [Fact] public void Publish_RejectsCourseWithoutLessons() =>
        TestData.Course().Invoking(x => x.Publish()).Should().Throw<DomainException>();

    [Fact] public void Publish_PublishesCourseWithLesson()
    {
        var course = TestData.Course(); course.AddLesson(TestData.Lesson(course)); course.Publish();
        course.Status.Should().Be(ContentStatus.Published);
    }

    [Fact] public void AddLesson_RejectsDuplicateLesson()
    {
        var course = TestData.Course(); var lesson = TestData.Lesson(course); course.AddLesson(lesson);
        course.Invoking(x => x.AddLesson(lesson)).Should().Throw<DomainException>();
    }

    [Fact] public void AddLesson_RejectsDuplicateOrder()
    {
        var course = TestData.Course(); course.AddLesson(TestData.Lesson(course));
        course.Invoking(x => x.AddLesson(new(course.Id, "Second", "second", LessonContentType.Video, 5, 0)))
            .Should().Throw<DomainException>();
    }

    [Fact] public void AddPrerequisite_RejectsSelfDependency() =>
        TestData.Course().Invoking(x => x.AddPrerequisite(x.Id)).Should().Throw<ArgumentException>();

    [Fact] public void AddPrerequisite_RejectsDuplicate()
    {
        var course = TestData.Course(); var prerequisiteId = Guid.NewGuid(); course.AddPrerequisite(prerequisiteId);
        course.Invoking(x => x.AddPrerequisite(prerequisiteId)).Should().Throw<DomainException>();
    }

    [Fact] public void AssociateSkill_RejectsDuplicate()
    {
        var course = TestData.Course(); var skillId = Guid.NewGuid(); course.AssociateSkill(skillId, SkillProficiencyLevel.Beginner);
        course.Invoking(x => x.AssociateSkill(skillId, SkillProficiencyLevel.Advanced)).Should().Throw<DomainException>();
    }
}
