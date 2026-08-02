using Careersity.Domain.Enums;

namespace Careersity.Application.AssessmentAttempts.Dtos;

public sealed record LearnerAssessmentSummaryDto(Guid AssessmentId, string Title, string? Description,
    int PassingScorePercentage, int? MaximumAttempts, int AttemptsUsed, int? AttemptsRemaining,
    bool HasPassed, Guid? ActiveAttemptId, int QuestionCount, int TotalPoints);
public sealed record LearnerAnswerOptionDto(Guid AnswerOptionId, string Text, int Order);
public sealed record LearnerAssessmentQuestionDto(Guid QuestionId, string Prompt, QuestionType QuestionType,
    int Order, int Points, IReadOnlyCollection<LearnerAnswerOptionDto> AnswerOptions);
public sealed record AssessmentAttemptStartDto(Guid AttemptId, Guid AssessmentId, int AttemptNumber,
    AssessmentAttemptStatus Status, DateTimeOffset StartedAtUtc, IReadOnlyCollection<LearnerAssessmentQuestionDto> Questions);
public sealed record LearnerAssessmentResponseDto(Guid QuestionId, IReadOnlyCollection<Guid> SelectedAnswerOptionIds,
    bool? IsCorrect, int? PointsAwarded);
public sealed record AssessmentAttemptDetailDto(Guid AttemptId, Guid AssessmentId, string AssessmentTitle,
    int AttemptNumber, AssessmentAttemptStatus Status, DateTimeOffset StartedAtUtc, DateTimeOffset? SubmittedAtUtc,
    decimal? ScorePercentage, int? PointsEarned, int? TotalPoints, bool Passed,
    IReadOnlyCollection<LearnerAssessmentResponseDto> Responses);
public sealed record AssessmentAttemptHistoryItemDto(Guid AttemptId, int AttemptNumber, AssessmentAttemptStatus Status,
    DateTimeOffset StartedAtUtc, DateTimeOffset? SubmittedAtUtc, decimal? ScorePercentage, bool Passed);
public sealed record StartAssessmentAttemptResult(AssessmentAttemptStartDto Attempt, bool Created);
