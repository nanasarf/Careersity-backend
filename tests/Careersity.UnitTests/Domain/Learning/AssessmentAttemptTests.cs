using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;
using Careersity.Domain.Learning;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Domain.Learning;

public sealed class AssessmentAttemptTests
{
    [Fact]
    public void Creation_RequiresIdentifiersAndPositiveNumber()
    {
        var attempt = Create();
        attempt.Status.Should().Be(AssessmentAttemptStatus.InProgress);
        attempt.AttemptNumber.Should().Be(1);
        attempt.StartedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        FluentActions.Invoking(() => new AssessmentAttempt(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new AssessmentAttempt(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0)).Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Responses_AreAddedReplacedAndRejectDuplicateOptions()
    {
        var attempt = Create(); var question = Guid.NewGuid(); var first = Guid.NewGuid(); var second = Guid.NewGuid();
        attempt.AddOrUpdateResponse(question, [first]);
        attempt.AddOrUpdateResponse(question, [second]);
        attempt.Responses.Should().ContainSingle();
        attempt.Responses.Single().SelectedOptions.Select(x => x.AnswerOptionId).Should().Equal(second);
        FluentActions.Invoking(() => attempt.AddOrUpdateResponse(question, [second, second])).Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Submit_RecordsImmutableGradeAndTerminalState(bool passed)
    {
        var attempt = Create(); var question = Guid.NewGuid(); attempt.AddOrUpdateResponse(question, [Guid.NewGuid()]);
        attempt.Submit(passed ? 70 : 69.99m, passed ? 7 : 0, 10, passed, new Dictionary<Guid, (bool, int)> { [question] = (passed, passed ? 7 : 0) });
        attempt.Status.Should().Be(passed ? AssessmentAttemptStatus.Passed : AssessmentAttemptStatus.Failed);
        attempt.SubmittedAtUtc.Should().NotBeNull();
        attempt.PassedAtUtc.HasValue.Should().Be(passed);
        attempt.Responses.Single().IsCorrect.Should().Be(passed);
        FluentActions.Invoking(() => attempt.AddOrUpdateResponse(question, [])).Should().Throw<DomainException>();
        FluentActions.Invoking(() => attempt.Submit(50, 5, 10, false, new Dictionary<Guid, (bool, int)>())).Should().Throw<DomainException>();
    }

    [Fact]
    public void Submit_RejectsInvalidGradeRanges()
    {
        var attempt = Create();
        FluentActions.Invoking(() => attempt.Submit(100.01m, 1, 1, true, new Dictionary<Guid, (bool, int)>())).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => attempt.Submit(50, -1, 1, false, new Dictionary<Guid, (bool, int)>())).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => attempt.Submit(50, 0, 0, false, new Dictionary<Guid, (bool, int)>())).Should().Throw<ArgumentOutOfRangeException>();
    }

    private static AssessmentAttempt Create() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
}
