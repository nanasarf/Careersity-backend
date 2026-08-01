using Careersity.Domain.Common;

namespace Careersity.Domain.Assessments;

/// <summary>A selectable answer belonging to an assessment question.</summary>
public sealed class AnswerOption : AuditableEntity
{
    private AnswerOption() { }

    public AnswerOption(Guid questionId, string text, bool isCorrect, int order)
    {
        QuestionId = Guard.NotEmpty(questionId, nameof(questionId));
        Text = Guard.Required(text, 1_000, nameof(text));
        IsCorrect = isCorrect;
        Order = Guard.NonNegative(order, nameof(order));
    }

    public Guid QuestionId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public bool IsCorrect { get; private set; }
    public int Order { get; private set; }

    public void SetCorrect(bool isCorrect) { IsCorrect = isCorrect; MarkUpdated(); }
    internal void UpdateText(string text) { Text = Guard.Required(text, 1_000, nameof(text)); MarkUpdated(); }
    internal void ChangeOrder(int order) { Order = Guard.NonNegative(order, nameof(order)); MarkUpdated(); }
}
