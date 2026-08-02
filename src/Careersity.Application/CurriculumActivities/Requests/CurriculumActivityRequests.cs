using Careersity.Domain.Enums;

namespace Careersity.Application.CurriculumActivities.Requests;

public sealed record CreateAssessmentRequest(Guid CourseId, string Title, string? Description, int PassingScorePercentage, int? MaximumAttempts);
public sealed record UpdateAssessmentRequest(string Title, string? Description, int PassingScorePercentage, int? MaximumAttempts);
public sealed record AddQuestionRequest(string Prompt, QuestionType QuestionType, int Order, int Points);
public sealed record UpdateQuestionRequest(string Prompt, QuestionType QuestionType, int Order, int Points);
public sealed record QuestionOrderItem(Guid QuestionId, int Order);
public sealed record ReorderQuestionsRequest(IReadOnlyCollection<QuestionOrderItem> Questions);
public sealed record AddAnswerOptionRequest(string Text, bool IsCorrect, int Order);
public sealed record UpdateAnswerOptionRequest(string Text, bool IsCorrect, int Order);
public sealed record AnswerOptionOrderItem(Guid AnswerOptionId, int Order);
public sealed record ReorderAnswerOptionsRequest(IReadOnlyCollection<AnswerOptionOrderItem> AnswerOptions);
public sealed record AssessmentQuery(int Page = 1, int PageSize = 20, string? Search = null, Guid? CourseId = null, ContentStatus? Status = null)
{ public int ValidatedPage => Page < 1 ? 1 : Page; public int ValidatedPageSize => Math.Clamp(PageSize, 1, 100); }
public sealed record CreateProjectRequest(Guid CourseId, string Title, string Description, string Instructions,
    string? ExpectedOutput, string? EvaluationCriteria, ProjectSubmissionType SubmissionType, int EstimatedDurationMinutes);
public sealed record UpdateProjectRequest(string Title, string Description, string Instructions,
    string? ExpectedOutput, string? EvaluationCriteria, ProjectSubmissionType SubmissionType, int EstimatedDurationMinutes);
public sealed record ProjectQuery(int Page = 1, int PageSize = 20, string? Search = null, Guid? CourseId = null,
    ProjectSubmissionType? SubmissionType = null, ContentStatus? Status = null)
{ public int ValidatedPage => Page < 1 ? 1 : Page; public int ValidatedPageSize => Math.Clamp(PageSize, 1, 100); }
