using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Domain.Courses;

public sealed class LessonTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveDuration(int minutes)
    {
        var act = () => new Lesson(Guid.NewGuid(), "Title", "slug", LessonContentType.Article, minutes, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] public void ExternalResource_RequiresUrl()
    {
        var act = () => new Lesson(Guid.NewGuid(), "Title", "slug", LessonContentType.ExternalResource, 5, 0);
        act.Should().Throw<ArgumentException>();
    }

    [Fact] public void ExternalResource_RejectsInvalidUrl()
    {
        var act = () => new Lesson(Guid.NewGuid(), "Title", "slug", LessonContentType.ExternalResource, 5, 0, externalResourceUrl: "not-a-url");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("http://example.com/resource")]
    [InlineData("https://example.com/resource")]
    public void ExternalResource_AcceptsAbsoluteHttpUrls(string url)
    {
        var lesson = new Lesson(Guid.NewGuid(), "Title", "slug", LessonContentType.ExternalResource, 5, 0, externalResourceUrl: url);
        lesson.ExternalResourceUrl.Should().Be(url);
    }
}
