namespace Careersity.Application.AssessmentAttempts.Requests;

public sealed record SaveAssessmentResponseRequest(Guid QuestionId, IReadOnlyCollection<Guid> SelectedAnswerOptionIds);
public sealed record SaveAssessmentResponsesRequest(IReadOnlyCollection<SaveAssessmentResponseRequest> Responses);
public sealed record SubmitAssessmentAttemptRequest(IReadOnlyCollection<SaveAssessmentResponseRequest>? Responses = null);
