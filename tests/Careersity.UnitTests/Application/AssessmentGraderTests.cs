using Careersity.Application.AssessmentAttempts.Services;
using Careersity.Domain.Assessments;
using Careersity.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Application;

public sealed class AssessmentGraderTests
{
    [Fact]
    public void GradesChoiceTypesWithExactSetAndRoundsToTwoDecimals()
    {
        var assessment = Guid.NewGuid();
        var single = Question(assessment, QuestionType.SingleChoice, 0, 2, [true, false]);
        var multiple = Question(assessment, QuestionType.MultipleChoice, 1, 3, [true, true, false]);
        var boolean = Question(assessment, QuestionType.TrueFalse, 2, 2, [true, false]);
        var answers = new Dictionary<Guid, IReadOnlyCollection<Guid>>
        {
            [single.Id] = [single.AnswerOptions.Single(x => x.IsCorrect).Id],
            [multiple.Id] = multiple.AnswerOptions.Where(x => x.IsCorrect).Select(x => x.Id).ToList(),
            [boolean.Id] = [boolean.AnswerOptions.Single(x => !x.IsCorrect).Id]
        };
        var result = AssessmentGrader.Grade([single, multiple, boolean], answers);
        result.Earned.Should().Be(5); result.Total.Should().Be(7); result.Score.Should().Be(71.43m);
        result.Grades[boolean.Id].IsCorrect.Should().BeFalse();
    }

    [Fact]
    public void MultipleChoiceMissingOrExtraOptionsReceivesNoCredit()
    {
        var question = Question(Guid.NewGuid(), QuestionType.MultipleChoice, 0, 5, [true, true, false]);
        var correct = question.AnswerOptions.Where(x => x.IsCorrect).ToList(); var incorrect = question.AnswerOptions.Single(x => !x.IsCorrect);
        AssessmentGrader.Grade([question], new Dictionary<Guid, IReadOnlyCollection<Guid>> { [question.Id] = [correct[0].Id] }).Earned.Should().Be(0);
        AssessmentGrader.Grade([question], new Dictionary<Guid, IReadOnlyCollection<Guid>> { [question.Id] = [correct[0].Id, correct[1].Id, incorrect.Id] }).Earned.Should().Be(0);
    }

    private static Question Question(Guid assessmentId, QuestionType type, int order, int points, IReadOnlyList<bool> correctness)
    {
        var question = new Question(assessmentId, "Prompt", type, order, points);
        for (var i = 0; i < correctness.Count; i++) question.AddAnswerOption(new AnswerOption(question.Id, $"Option {i}", correctness[i], i));
        return question;
    }
}
