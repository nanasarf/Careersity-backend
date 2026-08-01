using Careersity.Domain.Common;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.Projects;

/// <summary>A practical course deliverable with submission and evaluation guidance.</summary>
public sealed class Project : PublishableEntity
{
    private Project() { }

    public Project(Guid courseId, string title, string description, string instructions,
        ProjectSubmissionType submissionType, int estimatedDurationMinutes,
        string? expectedOutput = null, string? evaluationCriteria = null)
    {
        CourseId = Guard.NotEmpty(courseId, nameof(courseId));
        SetDetails(title, description, instructions, expectedOutput, evaluationCriteria);
        SubmissionType = submissionType;
        EstimatedDurationMinutes = Guard.Positive(estimatedDurationMinutes, nameof(estimatedDurationMinutes));
    }

    public Guid CourseId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Instructions { get; private set; } = string.Empty;
    public string? ExpectedOutput { get; private set; }
    public string? EvaluationCriteria { get; private set; }
    public ProjectSubmissionType SubmissionType { get; private set; }
    public int EstimatedDurationMinutes { get; private set; }

    public void UpdateDetails(string title, string description, string instructions,
        string? expectedOutput = null, string? evaluationCriteria = null)
    {
        SetDetails(title, description, instructions, expectedOutput, evaluationCriteria);
        MarkUpdated();
    }

    public void ChangeSubmissionType(ProjectSubmissionType submissionType) { SubmissionType = submissionType; MarkUpdated(); }
    public void ChangeDuration(int minutes) { EstimatedDurationMinutes = Guard.Positive(minutes, nameof(minutes)); MarkUpdated(); }

    public override void Publish()
    {
        if (Status == ContentStatus.Draft && string.IsNullOrWhiteSpace(Instructions))
            throw new DomainException("A project must contain instructions before publication.");
        base.Publish();
    }

    private void SetDetails(string title, string description, string instructions,
        string? expectedOutput, string? evaluationCriteria)
    {
        Title = Guard.Required(title, 200, nameof(title));
        Description = Guard.Required(description, 2_000, nameof(description));
        Instructions = Guard.Required(instructions, 10_000, nameof(instructions));
        ExpectedOutput = Guard.Optional(expectedOutput, 3_000, nameof(expectedOutput));
        EvaluationCriteria = Guard.Optional(evaluationCriteria, 5_000, nameof(evaluationCriteria));
    }
}
