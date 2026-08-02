using Careersity.Domain.Common;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.Assessments;

/// <summary>A scored choice-based question with ordered answer options.</summary>
public sealed class Question : AuditableEntity
{
    private readonly List<AnswerOption> _answerOptions = [];

    private Question() { }

    public Question(Guid assessmentId, string prompt, QuestionType questionType, int order, int points)
    {
        AssessmentId = Guard.NotEmpty(assessmentId, nameof(assessmentId));
        Prompt = Guard.Required(prompt, 2_000, nameof(prompt));
        QuestionType = questionType;
        Order = Guard.NonNegative(order, nameof(order));
        Points = Guard.Positive(points, nameof(points));
    }

    public Guid AssessmentId { get; private set; }
    public string Prompt { get; private set; } = string.Empty;
    public QuestionType QuestionType { get; private set; }
    public int Order { get; private set; }
    public int Points { get; private set; }
    public IReadOnlyCollection<AnswerOption> AnswerOptions => _answerOptions.AsReadOnly();

    public void UpdatePrompt(string prompt) { Prompt = Guard.Required(prompt, 2_000, nameof(prompt)); MarkUpdated(); }
    public void ChangePoints(int points) { Points = Guard.Positive(points, nameof(points)); MarkUpdated(); }
    public void ChangeType(QuestionType questionType) { QuestionType = questionType; MarkUpdated(); }

    public void AddAnswerOption(AnswerOption option)
    {
        ArgumentNullException.ThrowIfNull(option);
        if (option.QuestionId != Id) throw new DomainException("The answer option belongs to a different question.");
        if (_answerOptions.Any(x => x.Id == option.Id)) throw new DomainException("The answer option is already in this question.");
        if (_answerOptions.Any(x => x.Order == option.Order)) throw new DomainException("Answer option order must be unique within a question.");
        if (_answerOptions.Any(x => string.Equals(x.Text, option.Text, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("Answer option text must be unique within a question.");
        _answerOptions.Add(option); MarkUpdated();
    }

    public void RemoveAnswerOption(Guid optionId)
    {
        var option = _answerOptions.SingleOrDefault(x => x.Id == optionId) ?? throw new DomainException("The answer option does not belong to this question.");
        _answerOptions.Remove(option); MarkUpdated();
    }

    public void UpdateAnswerOptionText(Guid optionId, string text)
    {
        var option = _answerOptions.SingleOrDefault(x => x.Id == optionId)
            ?? throw new DomainException("The answer option does not belong to this question.");
        var normalizedText = Guard.Required(text, 1_000, nameof(text));
        if (_answerOptions.Any(x => x.Id != optionId && string.Equals(x.Text, normalizedText, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("Answer option text must be unique within a question.");
        option.UpdateText(normalizedText);
        MarkUpdated();
    }

    public void ReorderAnswerOption(Guid optionId, int newOrder)
    {
        Guard.NonNegative(newOrder, nameof(newOrder));
        var option = _answerOptions.SingleOrDefault(x => x.Id == optionId) ?? throw new DomainException("The answer option does not belong to this question.");
        if (_answerOptions.Any(x => x.Id != optionId && x.Order == newOrder)) throw new DomainException("Answer option order must be unique within a question.");
        option.ChangeOrder(newOrder); MarkUpdated();
    }

    /// <summary>Returns whether the answer set satisfies the rules for this question type.</summary>
    public bool IsValid()
    {
        var correctCount = _answerOptions.Count(x => x.IsCorrect);
        return QuestionType switch
        {
            QuestionType.SingleChoice => _answerOptions.Count >= 2 && correctCount == 1,
            QuestionType.MultipleChoice => _answerOptions.Count >= 2 && correctCount >= 1,
            QuestionType.TrueFalse => _answerOptions.Count == 2 && correctCount == 1,
            _ => false
        };
    }

    internal void ChangeOrder(int order) { Order = Guard.NonNegative(order, nameof(order)); MarkUpdated(); }
}
