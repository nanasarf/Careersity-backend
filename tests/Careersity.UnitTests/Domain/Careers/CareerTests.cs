using Careersity.Domain.Careers;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Domain.Careers;

public sealed class CareerTests
{
    [Fact] public void Constructor_CreatesDraft() => TestData.Career().Status.Should().Be(ContentStatus.Draft);

    [Fact] public void Constructor_RejectsEmptyTitle()
    {
        var act = () => new Career(Guid.NewGuid(), " ", "slug", "description");
        act.Should().Throw<ArgumentException>();
    }

    [Fact] public void Constructor_RejectsEmptyCategoryId()
    {
        var act = () => new Career(Guid.Empty, "Title", "slug", "description");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveEstimatedDuration(int weeks)
    {
        var act = () => TestData.Career(weeks);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact] public void Publish_PublishesDraft()
    {
        var career = TestData.Career(); career.Publish();
        career.Status.Should().Be(ContentStatus.Published);
    }

    [Fact] public void Publish_RejectsArchivedCareer()
    {
        var career = TestData.Career(); career.Archive();
        career.Invoking(x => x.Publish()).Should().Throw<DomainException>();
    }
}
