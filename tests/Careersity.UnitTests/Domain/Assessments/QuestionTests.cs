using Careersity.Domain.Assessments;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Careersity.UnitTests.Domain.Assessments;

public sealed class QuestionTests
{
    [Fact] public void SingleChoice_RequiresExactlyOneCorrectAnswer()
    {
        var question = CreateQuestion(QuestionType.SingleChoice);
        AddOption(question, "A", true, 0); AddOption(question, "B", true, 1);
        question.IsValid().Should().BeFalse();
        question.AnswerOptions.Last().SetCorrect(false);
        question.IsValid().Should().BeTrue();
    }

    [Fact] public void MultipleChoice_RequiresAtLeastOneCorrectAnswer()
    {
        var question = CreateQuestion(QuestionType.MultipleChoice);
        AddOption(question, "A", false, 0); AddOption(question, "B", false, 1);
        question.IsValid().Should().BeFalse();
        question.AnswerOptions.First().SetCorrect(true);
        question.IsValid().Should().BeTrue();
    }

    [Fact] public void TrueFalse_RequiresExactlyTwoAnswers()
    {
        var question = CreateQuestion(QuestionType.TrueFalse);
        AddOption(question, "True", true, 0);
        question.IsValid().Should().BeFalse();
        AddOption(question, "False", false, 1);
        question.IsValid().Should().BeTrue();
        AddOption(question, "Unknown", false, 2);
        question.IsValid().Should().BeFalse();
    }

    [Fact] public void AddAnswerOption_RejectsDuplicateTextCaseInsensitively()
    {
        var question = CreateQuestion(QuestionType.SingleChoice); AddOption(question, "Answer", true, 0);
        question.Invoking(x => x.AddAnswerOption(new AnswerOption(x.Id, "answer", false, 1))).Should().Throw<DomainException>();
    }

    [Fact] public void AddAnswerOption_RejectsDuplicateOrder()
    {
        var question = CreateQuestion(QuestionType.SingleChoice); AddOption(question, "A", true, 0);
        question.Invoking(x => x.AddAnswerOption(new AnswerOption(x.Id, "B", false, 0))).Should().Throw<DomainException>();
    }

    [Fact] public void UpdateAnswerOptionText_RejectsDuplicateTextCaseInsensitively()
    {
        var question = CreateQuestion(QuestionType.SingleChoice);
        AddOption(question, "A", true, 0); AddOption(question, "B", false, 1);
        var secondOption = question.AnswerOptions.Last();
        question.Invoking(x => x.UpdateAnswerOptionText(secondOption.Id, "a")).Should().Throw<DomainException>();
    }

    private static Question CreateQuestion(QuestionType type) => new(Guid.NewGuid(), "Prompt", type, 0, 1);
    private static void AddOption(Question question, string text, bool correct, int order) =>
        question.AddAnswerOption(new AnswerOption(question.Id, text, correct, order));
}
