using Careersity.Application.Common.Models;
using Careersity.Application.LearningContent.Dtos;
using Careersity.Application.LearningContent.Requests;

namespace Careersity.Application.LearningContent.Services;

public interface ISkillService
{
    Task<SkillDto> CreateAsync(CreateSkillRequest request, CancellationToken cancellationToken);
    Task<SkillDto> UpdateAsync(Guid id, UpdateSkillRequest request, CancellationToken cancellationToken);
    Task PublishAsync(Guid id, CancellationToken cancellationToken); Task ArchiveAsync(Guid id, CancellationToken cancellationToken);
    Task<SkillDto> GetAdminAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<SkillListItemDto>> ListAdminAsync(SkillQuery query, CancellationToken cancellationToken);
    Task<PagedResult<SkillListItemDto>> ListPublishedAsync(SkillQuery query, CancellationToken cancellationToken);
    Task<SkillDto> GetPublishedAsync(string slug, CancellationToken cancellationToken);
}
public interface ICourseService
{
    Task<CourseDetailDto> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken);
    Task<CourseDetailDto> UpdateAsync(Guid id, UpdateCourseRequest request, CancellationToken cancellationToken);
    Task PublishAsync(Guid id, CancellationToken cancellationToken); Task ArchiveAsync(Guid id, CancellationToken cancellationToken);
    Task<CourseDetailDto> GetAdminAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<CourseListItemDto>> ListAdminAsync(CourseQuery query, CancellationToken cancellationToken);
    Task<PagedResult<CourseListItemDto>> ListPublishedAsync(CourseQuery query, CancellationToken cancellationToken);
    Task<CourseDetailDto> GetPublishedAsync(string slug, CancellationToken cancellationToken);
}
public interface ILessonService
{
    Task<LessonDto> AddAsync(Guid courseId, AddLessonRequest request, CancellationToken cancellationToken);
    Task<LessonDto> UpdateAsync(Guid courseId, Guid lessonId, UpdateLessonRequest request, CancellationToken cancellationToken);
    Task RemoveAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken);
    Task ReorderAsync(Guid courseId, ReorderLessonsRequest request, CancellationToken cancellationToken);
    Task<LessonDto> GetAdminAsync(Guid courseId, Guid lessonId, CancellationToken cancellationToken);
    Task<LessonDto> GetPublishedAsync(string courseSlug, string lessonSlug, CancellationToken cancellationToken);
}
public interface ICoursePrerequisiteService
{
    Task<CoursePrerequisiteDto> AddAsync(Guid courseId, AddCoursePrerequisiteRequest request, CancellationToken cancellationToken);
    Task<CoursePrerequisiteDto> UpdateAsync(Guid courseId, Guid relationshipId, UpdateCoursePrerequisiteRequest request, CancellationToken cancellationToken);
    Task RemoveAsync(Guid courseId, Guid relationshipId, CancellationToken cancellationToken);
}
public interface ICourseSkillService
{
    Task<CourseSkillDto> AddAsync(Guid courseId, AddCourseSkillRequest request, CancellationToken cancellationToken);
    Task<CourseSkillDto> UpdateAsync(Guid courseId, Guid relationshipId, UpdateCourseSkillRequest request, CancellationToken cancellationToken);
    Task RemoveAsync(Guid courseId, Guid relationshipId, CancellationToken cancellationToken);
}
