using Careersity.Domain.Common;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.Assessments;

/// <summary>A publishable collection of scored questions belonging to a course.</summary>
public sealed class Assessment : PublishableEntity
{
    private readonly List<Question> _questions = [];

    private Assessment() { }

    public Assessment(Guid courseId, string title, int passingScorePercentage,
        int? maximumAttempts = null, string? description = null)
    {
        CourseId = Guard.NotEmpty(courseId, nameof(courseId));
        Title = Guard.Required(title, 200, nameof(title));
        Description = Guard.Optional(description, 2_000, nameof(description));
        SetPassingScore(passingScorePercentage);
        SetMaximumAttempts(maximumAttempts);
    }

    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int PassingScorePercentage { get; private set; }
    public int? MaximumAttempts { get; private set; }
    public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

    public void UpdateDetails(string title, string? description = null)
    {
        Title = Guard.Required(title, 200, nameof(title));
        Description = Guard.Optional(description, 2_000, nameof(description));
        MarkUpdated();
    }

    public void ChangePassingScore(int percentage) { SetPassingScore(percentage); MarkUpdated(); }
    public void ChangeMaximumAttempts(int? attempts) { SetMaximumAttempts(attempts); MarkUpdated(); }

    public void AddQuestion(Question question)
    {
        ArgumentNullException.ThrowIfNull(question);
        if (question.AssessmentId != Id) throw new DomainException("The question belongs to a different assessment.");
        if (_questions.Any(x => x.Id == question.Id)) throw new DomainException("The question is already in this assessment.");
        if (_questions.Any(x => x.Order == question.Order)) throw new DomainException("Question order must be unique within an assessment.");
        _questions.Add(question); MarkUpdated();
    }

    public void RemoveQuestion(Guid questionId)
    {
        var question = _questions.SingleOrDefault(x => x.Id == questionId) ?? throw new DomainException("The question does not belong to this assessment.");
        _questions.Remove(question); MarkUpdated();
    }

    public void ReorderQuestion(Guid questionId, int newOrder)
    {
        Guard.NonNegative(newOrder, nameof(newOrder));
        var question = _questions.SingleOrDefault(x => x.Id == questionId) ?? throw new DomainException("The question does not belong to this assessment.");
        if (_questions.Any(x => x.Id != questionId && x.Order == newOrder)) throw new DomainException("Question order must be unique within an assessment.");
        question.ChangeOrder(newOrder); MarkUpdated();
    }

    public override void Publish()
    {
        if (Status == ContentStatus.Draft)
        {
            if (_questions.Count == 0) throw new DomainException("An assessment must contain at least one question before publication.");
            if (_questions.Any(x => !x.IsValid())) throw new DomainException("Every assessment question must be valid before publication.");
        }
        base.Publish();
    }

    private void SetPassingScore(int percentage)
    {
        if (percentage is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(percentage), "Passing score must be between 1 and 100.");
        PassingScorePercentage = percentage;
    }

    private void SetMaximumAttempts(int? attempts)
    {
        if (attempts.HasValue) Guard.Positive(attempts.Value, nameof(attempts));
        MaximumAttempts = attempts;
    }
}
