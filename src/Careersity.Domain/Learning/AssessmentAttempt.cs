using Careersity.Domain.Common;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.Learning;

public sealed class AssessmentAttempt : AuditableEntity
{
    private readonly List<AssessmentResponse> _responses = [];
    private AssessmentAttempt() { }
    public AssessmentAttempt(Guid userId, Guid careerEnrollmentId, Guid courseProgressId, Guid assessmentId, int attemptNumber)
    {
        UserId = Guard.NotEmpty(userId, nameof(userId));
        CareerEnrollmentId = Guard.NotEmpty(careerEnrollmentId, nameof(careerEnrollmentId));
        CourseProgressId = Guard.NotEmpty(courseProgressId, nameof(courseProgressId));
        AssessmentId = Guard.NotEmpty(assessmentId, nameof(assessmentId));
        AttemptNumber = Guard.Positive(attemptNumber, nameof(attemptNumber));
        Status = AssessmentAttemptStatus.InProgress;
        StartedAtUtc = DateTimeOffset.UtcNow;
    }
    public Guid UserId { get; private set; }
    public Guid CareerEnrollmentId { get; private set; }
    public Guid CourseProgressId { get; private set; }
    public Guid AssessmentId { get; private set; }
    public int AttemptNumber { get; private set; }
    public AssessmentAttemptStatus Status { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public decimal? ScorePercentage { get; private set; }
    public int? PointsEarned { get; private set; }
    public int? TotalPoints { get; private set; }
    public DateTimeOffset? PassedAtUtc { get; private set; }
    public IReadOnlyCollection<AssessmentResponse> Responses => _responses.AsReadOnly();

    public void AddOrUpdateResponse(Guid questionId, IEnumerable<Guid> optionIds)
    {
        EnsureInProgress();
        Guard.NotEmpty(questionId, nameof(questionId));
        var existing = _responses.SingleOrDefault(x => x.QuestionId == questionId);
        if (existing is null) _responses.Add(new AssessmentResponse(Id, questionId, optionIds));
        else existing.ReplaceOptions(optionIds);
        MarkUpdated();
    }

    public void Submit(decimal scorePercentage, int pointsEarned, int totalPoints, bool passed,
        IReadOnlyDictionary<Guid, (bool IsCorrect, int PointsAwarded)> responseGrades)
    {
        EnsureInProgress();
        if (scorePercentage is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(scorePercentage));
        if (pointsEarned < 0) throw new ArgumentOutOfRangeException(nameof(pointsEarned));
        if (totalPoints <= 0) throw new ArgumentOutOfRangeException(nameof(totalPoints));
        if (pointsEarned > totalPoints) throw new ArgumentOutOfRangeException(nameof(pointsEarned));
        foreach (var response in _responses)
        {
            if (!responseGrades.TryGetValue(response.QuestionId, out var grade)) throw new DomainException("A grade is required for every response.");
            response.RecordGrade(grade.IsCorrect, grade.PointsAwarded);
        }
        var now = DateTimeOffset.UtcNow;
        ScorePercentage = scorePercentage; PointsEarned = pointsEarned; TotalPoints = totalPoints;
        SubmittedAtUtc = now;
        Status = passed ? AssessmentAttemptStatus.Passed : AssessmentAttemptStatus.Failed;
        PassedAtUtc = passed ? now : null;
        MarkUpdated();
    }

    private void EnsureInProgress()
    {
        if (Status != AssessmentAttemptStatus.InProgress) throw new DomainException("A completed assessment attempt cannot be edited or submitted again.");
    }
}
