using Careersity.Application.AssessmentAttempts.Dtos;
using Careersity.Application.AssessmentAttempts.Requests;

namespace Careersity.Application.AssessmentAttempts.Services;

public interface IAssessmentAttemptService
{
    Task<IReadOnlyCollection<LearnerAssessmentSummaryDto>> ListAsync(Guid enrollmentId, Guid courseId, CancellationToken token);
    Task<StartAssessmentAttemptResult> StartAsync(Guid enrollmentId, Guid courseId, Guid assessmentId, CancellationToken token);
    Task<IReadOnlyCollection<AssessmentAttemptHistoryItemDto>> HistoryAsync(Guid enrollmentId, Guid courseId, Guid assessmentId, CancellationToken token);
    Task<AssessmentAttemptDetailDto> GetAsync(Guid enrollmentId, Guid courseId, Guid assessmentId, Guid attemptId, CancellationToken token);
    Task<AssessmentAttemptDetailDto> SaveAsync(Guid enrollmentId, Guid courseId, Guid assessmentId, Guid attemptId, SaveAssessmentResponsesRequest request, CancellationToken token);
    Task<AssessmentAttemptDetailDto> SubmitAsync(Guid enrollmentId, Guid courseId, Guid assessmentId, Guid attemptId, SubmitAssessmentAttemptRequest request, CancellationToken token);
}
