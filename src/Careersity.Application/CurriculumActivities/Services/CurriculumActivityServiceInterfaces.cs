using Careersity.Application.Common.Models;
using Careersity.Application.CurriculumActivities.Dtos;
using Careersity.Application.CurriculumActivities.Requests;

namespace Careersity.Application.CurriculumActivities.Services;

public interface IAssessmentService
{
    Task<AssessmentAdminDetailDto> CreateAsync(CreateAssessmentRequest request, CancellationToken cancellationToken);
    Task<AssessmentAdminDetailDto> UpdateAsync(Guid id, UpdateAssessmentRequest request, CancellationToken cancellationToken);
    Task PublishAsync(Guid id, CancellationToken cancellationToken); Task ArchiveAsync(Guid id, CancellationToken cancellationToken);
    Task<AssessmentAdminDetailDto> GetAdminAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<AssessmentListItemDto>> ListAdminAsync(AssessmentQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PublicAssessmentSummaryDto>> ListPublishedAsync(string courseSlug, CancellationToken cancellationToken);
}
public interface IQuestionService
{
    Task<QuestionAdminDto> AddAsync(Guid assessmentId, AddQuestionRequest request, CancellationToken cancellationToken);
    Task<QuestionAdminDto> UpdateAsync(Guid assessmentId, Guid questionId, UpdateQuestionRequest request, CancellationToken cancellationToken);
    Task RemoveAsync(Guid assessmentId, Guid questionId, CancellationToken cancellationToken);
    Task ReorderAsync(Guid assessmentId, ReorderQuestionsRequest request, CancellationToken cancellationToken);
    Task<QuestionAdminDto> GetAdminAsync(Guid assessmentId, Guid questionId, CancellationToken cancellationToken);
}
public interface IAnswerOptionService
{
    Task<AnswerOptionAdminDto> AddAsync(Guid assessmentId, Guid questionId, AddAnswerOptionRequest request, CancellationToken cancellationToken);
    Task<AnswerOptionAdminDto> UpdateAsync(Guid assessmentId, Guid questionId, Guid optionId, UpdateAnswerOptionRequest request, CancellationToken cancellationToken);
    Task RemoveAsync(Guid assessmentId, Guid questionId, Guid optionId, CancellationToken cancellationToken);
    Task ReorderAsync(Guid assessmentId, Guid questionId, ReorderAnswerOptionsRequest request, CancellationToken cancellationToken);
}
public interface IProjectService
{
    Task<ProjectAdminDetailDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken);
    Task<ProjectAdminDetailDto> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken);
    Task PublishAsync(Guid id, CancellationToken cancellationToken); Task ArchiveAsync(Guid id, CancellationToken cancellationToken);
    Task<ProjectAdminDetailDto> GetAdminAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<ProjectListItemDto>> ListAdminAsync(ProjectQuery query, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PublicProjectSummaryDto>> ListPublishedAsync(string courseSlug, CancellationToken cancellationToken);
    Task<PublicProjectDetailDto> GetPublishedAsync(string courseSlug, Guid projectId, CancellationToken cancellationToken);
}
