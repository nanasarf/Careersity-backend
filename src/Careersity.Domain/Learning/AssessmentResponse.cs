using Careersity.Domain.Common;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.Learning;

public sealed class AssessmentResponse : AuditableEntity
{
    private readonly List<AssessmentResponseOption> _selectedOptions = [];
    private AssessmentResponse() { }
    internal AssessmentResponse(Guid assessmentAttemptId, Guid questionId, IEnumerable<Guid> selectedAnswerOptionIds)
    {
        AssessmentAttemptId = Guard.NotEmpty(assessmentAttemptId, nameof(assessmentAttemptId));
        QuestionId = Guard.NotEmpty(questionId, nameof(questionId));
        ReplaceOptions(selectedAnswerOptionIds);
    }
    public Guid AssessmentAttemptId { get; private set; }
    public Guid QuestionId { get; private set; }
    public bool? IsCorrect { get; private set; }
    public int? PointsAwarded { get; private set; }
    public IReadOnlyCollection<AssessmentResponseOption> SelectedOptions => _selectedOptions.AsReadOnly();

    internal void ReplaceOptions(IEnumerable<Guid> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        var values = ids.ToList();
        if (values.Any(x => x == Guid.Empty)) throw new ArgumentException("Selected answer option IDs cannot be empty.", nameof(ids));
        if (values.Distinct().Count() != values.Count) throw new DomainException("Selected answer option IDs cannot contain duplicates.");
        _selectedOptions.Clear();
        _selectedOptions.AddRange(values.Select(x => new AssessmentResponseOption(Id, x)));
        MarkUpdated();
    }
    internal void RecordGrade(bool correct, int points)
    {
        if (IsCorrect.HasValue) throw new DomainException("The response has already been graded.");
        if (points < 0) throw new ArgumentOutOfRangeException(nameof(points));
        IsCorrect = correct; PointsAwarded = points; MarkUpdated();
    }
}
