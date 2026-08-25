using Careersity.Application.CareerCatalog.Dtos;
using Careersity.Application.CareerCatalog.Requests;
using Careersity.Application.Common.Models;

namespace Careersity.Application.CareerCatalog.Services;

public interface ICareerCategoryService
{
    Task<CareerCategoryDto> CreateAsync(CreateCareerCategoryRequest request, CancellationToken cancellationToken);
    Task<CareerCategoryDto> UpdateAsync(Guid id, UpdateCareerCategoryRequest request, CancellationToken cancellationToken);
    Task PublishAsync(Guid id, CancellationToken cancellationToken);
    Task ArchiveAsync(Guid id, CancellationToken cancellationToken);
    Task<CareerCategoryDto> GetAdminAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CareerCategoryDto>> ListAdminAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CareerCategoryDto>> ListPublishedAsync(CancellationToken cancellationToken);
}

public interface ICareerService
{
    Task<CareerDetailDto> CreateAsync(CreateCareerRequest request, CancellationToken cancellationToken);
    Task<CareerDetailDto> UpdateAsync(Guid id, UpdateCareerRequest request, CancellationToken cancellationToken);
    Task ChangeCategoryAsync(Guid id, ChangeCareerCategoryRequest request, CancellationToken cancellationToken);
    Task PublishAsync(Guid id, CancellationToken cancellationToken);
    Task ArchiveAsync(Guid id, CancellationToken cancellationToken);
    Task<CareerReadinessDto> GetReadinessAsync(Guid id, CancellationToken cancellationToken);
    Task<CareerDetailDto> GetAdminAsync(Guid id, CancellationToken cancellationToken);
    Task<PagedResult<CareerListItemDto>> ListAdminAsync(CareerQuery query, CancellationToken cancellationToken);
    Task<PagedResult<CareerListItemDto>> ListPublishedAsync(CareerQuery query, CancellationToken cancellationToken);
    Task<CareerDetailDto> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken);
}

public interface ICareerSkillService
{
    Task<CareerSkillDto> AssignAsync(Guid careerId, AssignCareerSkillRequest request, CancellationToken cancellationToken);
    Task<CareerSkillDto> UpdateAsync(Guid careerId, Guid careerSkillId, UpdateCareerSkillRequest request, CancellationToken cancellationToken);
    Task RemoveAsync(Guid careerId, Guid careerSkillId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CareerSkillDto>> ListAsync(Guid careerId, bool publishedOnly, CancellationToken cancellationToken);
}

public interface ICareerPathwayService
{
    Task<CareerPathwayDto> CreateAsync(Guid careerId, CreateCareerPathwayRequest request, CancellationToken cancellationToken);
    Task<CareerPathwayDto> UpdateAsync(Guid careerId, Guid pathwayId, UpdateCareerPathwayRequest request, CancellationToken cancellationToken);
    Task PublishAsync(Guid careerId, Guid pathwayId, CancellationToken cancellationToken);
    Task ArchiveAsync(Guid careerId, Guid pathwayId, CancellationToken cancellationToken);
    Task<CareerPathwayDto> GetAdminAsync(Guid careerId, Guid pathwayId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CareerPathwayDto>> ListAdminAsync(Guid careerId, CancellationToken cancellationToken);
    Task<CareerPathwayDto> GetPublicPrimaryAsync(Guid careerId, CancellationToken cancellationToken);
    Task<PathwayLevelDto> AddLevelAsync(Guid careerId, Guid pathwayId, AddPathwayLevelRequest request, CancellationToken cancellationToken);
    Task<PathwayLevelDto> UpdateLevelAsync(Guid careerId, Guid pathwayId, Guid levelId, UpdatePathwayLevelRequest request, CancellationToken cancellationToken);
    Task RemoveLevelAsync(Guid careerId, Guid pathwayId, Guid levelId, CancellationToken cancellationToken);
    Task ReorderLevelsAsync(Guid careerId, Guid pathwayId, ReorderPathwayLevelsRequest request, CancellationToken cancellationToken);
    Task<PathwayLevelCourseDto> AddCourseAsync(Guid careerId, Guid pathwayId, Guid levelId, AddPathwayLevelCourseRequest request, CancellationToken cancellationToken);
    Task<PathwayLevelCourseDto> UpdateCourseAsync(Guid careerId, Guid pathwayId, Guid levelId, Guid assignmentId, UpdatePathwayLevelCourseRequest request, CancellationToken cancellationToken);
    Task RemoveCourseAsync(Guid careerId, Guid pathwayId, Guid levelId, Guid assignmentId, CancellationToken cancellationToken);
    Task ReorderCoursesAsync(Guid careerId, Guid pathwayId, Guid levelId, ReorderPathwayCoursesRequest request, CancellationToken cancellationToken);
}
