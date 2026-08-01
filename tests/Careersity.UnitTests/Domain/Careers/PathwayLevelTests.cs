using Careersity.Domain.Careers;
using Careersity.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Domain.Careers;

public sealed class PathwayLevelTests
{
    private static PathwayLevel CreateLevel() => new(Guid.NewGuid(), "Foundation", 0);

    [Fact] public void AddCourse_AddsCourse()
    {
        var level = CreateLevel(); var courseId = Guid.NewGuid();
        level.AddCourse(courseId, 0);
        level.Courses.Should().ContainSingle(x => x.CourseId == courseId);
    }

    [Fact] public void AddCourse_RejectsDuplicateCourse()
    {
        var level = CreateLevel(); var courseId = Guid.NewGuid(); level.AddCourse(courseId, 0);
        level.Invoking(x => x.AddCourse(courseId, 1)).Should().Throw<DomainException>();
    }

    [Fact] public void AddCourse_RejectsDuplicateOrder()
    {
        var level = CreateLevel(); level.AddCourse(Guid.NewGuid(), 0);
        level.Invoking(x => x.AddCourse(Guid.NewGuid(), 0)).Should().Throw<DomainException>();
    }

    [Fact] public void ReorderCourse_ChangesOrder()
    {
        var level = CreateLevel(); var courseId = Guid.NewGuid(); level.AddCourse(courseId, 0);
        level.ReorderCourse(courseId, 3);
        level.Courses.Single().Order.Should().Be(3);
    }
}
