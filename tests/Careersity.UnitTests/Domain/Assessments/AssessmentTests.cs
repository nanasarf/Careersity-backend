using Careersity.Domain.Assessments;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Domain.Assessments;

public sealed class AssessmentTests
{
    [Fact] public void Publish_RejectsAssessmentWithoutQuestions() =>
        TestData.Assessment().Invoking(x => x.Publish()).Should().Throw<DomainException>();

    [Fact] public void Publish_PublishesAssessmentWithValidQuestion()
    {
        var assessment = TestData.Assessment(); assessment.AddQuestion(TestData.ValidSingleChoice(assessment));
        assessment.Publish();
        assessment.Status.Should().Be(ContentStatus.Published);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Constructor_RejectsPassingScoreOutsideRange(int score)
    {
        var act = () => new Assessment(Guid.NewGuid(), "Title", score);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveMaximumAttempts(int attempts)
    {
        var act = () => new Assessment(Guid.NewGuid(), "Title", 70, attempts);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
