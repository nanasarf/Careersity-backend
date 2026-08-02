using Careersity.Application.Common.Models;
using Careersity.Application.LearningProgress.Dtos;
using Careersity.Application.LearningProgress.Requests;

namespace Careersity.Application.LearningProgress.Services;

public interface ILearningProgressService
{
    Task<CareerEnrollmentDetailDto> EnrollAsync(EnrollInCareerRequest request, CancellationToken token);
    Task<PagedResult<CareerEnrollmentListItemDto>> ListAsync(EnrollmentListQuery query, CancellationToken token);
    Task<CareerEnrollmentDetailDto> GetAsync(Guid enrollmentId, CancellationToken token);
    Task PauseAsync(Guid enrollmentId, CancellationToken token);
    Task ResumeAsync(Guid enrollmentId, CancellationToken token);
    Task WithdrawAsync(Guid enrollmentId, CancellationToken token);
    Task<LearnerCourseDetailDto> GetCourseAsync(Guid enrollmentId, Guid courseId, CancellationToken token);
    Task<LearnerCourseDetailDto> StartCourseAsync(Guid enrollmentId, Guid courseId, CancellationToken token);
    Task<LearnerCourseDetailDto> CompleteCourseAsync(Guid enrollmentId, Guid courseId, CancellationToken token);
    Task<LearnerLessonDetailDto> GetLessonAsync(Guid enrollmentId, Guid courseId, Guid lessonId, CancellationToken token);
    Task<LearnerLessonDetailDto> StartLessonAsync(Guid enrollmentId, Guid courseId, Guid lessonId, CancellationToken token);
    Task<LearnerLessonDetailDto> CompleteLessonAsync(Guid enrollmentId, Guid courseId, Guid lessonId, CancellationToken token);
}
