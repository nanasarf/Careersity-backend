using Careersity.Application.Common.Models;
using Careersity.Application.ExternalLearning.Dtos;
using Careersity.Application.ExternalLearning.Requests;

namespace Careersity.Application.ExternalLearning.Services;

public interface ILearningProviderService
{
    Task<LearningProviderDto> CreateAsync(CreateLearningProviderRequest request, CancellationToken token); Task<LearningProviderDto> UpdateAsync(Guid id, UpdateLearningProviderRequest request, CancellationToken token);
    Task PublishAsync(Guid id, CancellationToken token); Task ArchiveAsync(Guid id, CancellationToken token); Task<LearningProviderDto> GetAdminAsync(Guid id, CancellationToken token);
    Task<PagedResult<LearningProviderDto>> ListAdminAsync(ExternalLearningQuery query, CancellationToken token); Task<IReadOnlyCollection<LearningProviderDto>> ListPublishedAsync(CancellationToken token); Task<LearningProviderDto> GetPublishedAsync(string slug, CancellationToken token);
}
public interface IInstructorService
{
    Task<InstructorDto> CreateAsync(CreateInstructorRequest request, CancellationToken token); Task<InstructorDto> UpdateAsync(Guid id, UpdateInstructorRequest request, CancellationToken token); Task<InstructorDto> ChangeProviderAsync(Guid id, ChangeInstructorProviderRequest request, CancellationToken token);
    Task PublishAsync(Guid id, CancellationToken token); Task ArchiveAsync(Guid id, CancellationToken token); Task<InstructorDto> GetAdminAsync(Guid id, CancellationToken token); Task<PagedResult<InstructorDto>> ListAdminAsync(ExternalLearningQuery query, CancellationToken token); Task<IReadOnlyCollection<InstructorDto>> ListPublishedAsync(Guid providerId, CancellationToken token);
}
public interface IExternalLearningResourceService
{
    Task<ExternalLearningResourceDetailDto> CreateAsync(CreateExternalLearningResourceRequest request, CancellationToken token); Task<ExternalLearningResourceDetailDto> UpdateAsync(Guid id, UpdateExternalLearningResourceRequest request, CancellationToken token);
    Task PublishAsync(Guid id, CancellationToken token); Task ArchiveAsync(Guid id, CancellationToken token); Task MarkReviewedAsync(Guid id, CancellationToken token); Task<ExternalLearningResourceDetailDto> GetAdminAsync(Guid id, CancellationToken token); Task<PagedResult<ExternalLearningResourceListItemDto>> ListAdminAsync(ExternalLearningQuery query, CancellationToken token); Task<IReadOnlyCollection<ExternalLearningResourceListItemDto>> ListPublishedAsync(CancellationToken token); Task<ExternalLearningResourceDetailDto> GetPublishedAsync(Guid id, CancellationToken token);
}
public interface ICourseExternalResourceService
{
    Task<CourseExternalResourceDto> AssignAsync(Guid courseId, AssignExternalResourceToCourseRequest request, CancellationToken token); Task<CourseExternalResourceDto> UpdateAsync(Guid courseId, Guid assignmentId, UpdateCourseExternalResourceRequest request, CancellationToken token); Task RemoveAsync(Guid courseId, Guid assignmentId, CancellationToken token); Task ReorderAsync(Guid courseId, ReorderCourseExternalResourcesRequest request, CancellationToken token); Task<IReadOnlyCollection<CourseExternalResourceDto>> ListAdminAsync(Guid courseId, CancellationToken token); Task<IReadOnlyCollection<PublicExternalResourceDto>> ListPublishedAsync(string courseSlug, CancellationToken token);
}
public interface ILearnerExternalResourceProgressService
{
    Task<IReadOnlyCollection<LearnerExternalResourceProgressDto>> ListAsync(Guid enrollmentId, Guid courseId, CancellationToken token); Task<LearnerExternalResourceProgressDto> StartAsync(Guid enrollmentId, Guid courseId, Guid assignmentId, CancellationToken token); Task<LearnerExternalResourceProgressDto> CompleteAsync(Guid enrollmentId, Guid courseId, Guid assignmentId, CancellationToken token);
}
