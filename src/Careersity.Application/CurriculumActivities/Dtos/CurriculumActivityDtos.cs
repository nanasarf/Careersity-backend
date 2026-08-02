using Careersity.Domain.Enums;

namespace Careersity.Application.CurriculumActivities.Dtos;

public sealed record AnswerOptionAdminDto(Guid Id, Guid QuestionId, string Text, bool IsCorrect, int Order,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record QuestionAdminDto(Guid Id, Guid AssessmentId, string Prompt, QuestionType QuestionType,
    int Order, int Points, IReadOnlyCollection<AnswerOptionAdminDto> AnswerOptions,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record AssessmentListItemDto(Guid Id, Guid CourseId, string Title, string? Description,
    int PassingScorePercentage, int? MaximumAttempts, ContentStatus Status, int QuestionCount, int TotalPoints);
public sealed record AssessmentAdminDetailDto(Guid Id, Guid CourseId, string CourseTitle, string Title,
    string? Description, int PassingScorePercentage, int? MaximumAttempts, ContentStatus Status,
    IReadOnlyCollection<QuestionAdminDto> Questions, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record PublicAssessmentSummaryDto(Guid Id, string Title, string? Description,
    int PassingScorePercentage, int? MaximumAttempts, int QuestionCount, int TotalPoints);
public sealed record ProjectListItemDto(Guid Id, Guid CourseId, string CourseTitle, string Title, string Description,
    ProjectSubmissionType SubmissionType, int EstimatedDurationMinutes, ContentStatus Status);
public sealed record ProjectAdminDetailDto(Guid Id, Guid CourseId, string CourseTitle, string Title, string Description,
    string Instructions, string? ExpectedOutput, string? EvaluationCriteria, ProjectSubmissionType SubmissionType,
    int EstimatedDurationMinutes, ContentStatus Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record PublicProjectSummaryDto(Guid Id, string Title, string Description,
    ProjectSubmissionType SubmissionType, int EstimatedDurationMinutes);
public sealed record PublicProjectDetailDto(Guid Id, Guid CourseId, string CourseTitle, string CourseSlug,
    string Title, string Description, string Instructions, string? ExpectedOutput, string? EvaluationCriteria,
    ProjectSubmissionType SubmissionType, int EstimatedDurationMinutes);
