using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.CareerCatalog.Dtos;
using Careersity.Domain.Careers;
using Careersity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.CareerCatalog.Services;

internal static class CareerCatalogProjections
{
    internal static CareerCategoryDto ToDto(CareerCategory entity) =>
        new(entity.Id, entity.Name, entity.Slug, entity.Description, entity.Status, entity.CreatedAtUtc, entity.UpdatedAtUtc);

    internal static async Task<IReadOnlyCollection<CareerSkillDto>> LoadSkillsAsync(
        ICareersityDbContext db, Guid careerId, bool publishedOnly, CancellationToken cancellationToken)
    {
        var query = from assignment in db.CareerSkills.AsNoTracking()
                    join skill in db.Skills.AsNoTracking() on assignment.SkillId equals skill.Id
                    where assignment.CareerId == careerId && (!publishedOnly || skill.Status == ContentStatus.Published)
                    orderby assignment.DisplayOrder
                    select new CareerSkillDto(assignment.Id, skill.Id, skill.Name, skill.Slug, skill.Category,
                        assignment.RequiredProficiencyLevel, assignment.IsRequired, assignment.DisplayOrder);
        return await query.ToListAsync(cancellationToken);
    }

    internal static async Task<CareerPathwayDto> LoadPathwayAsync(
        ICareersityDbContext db, CareerPathway pathway, bool publishedOnly, CancellationToken cancellationToken)
    {
        var levels = await db.PathwayLevels.AsNoTracking().Where(x => x.CareerPathwayId == pathway.Id)
            .OrderBy(x => x.Order).Select(x => new { x.Id, x.Name, x.Description, x.Order }).ToListAsync(cancellationToken);
        var levelIds = levels.Select(x => x.Id).ToArray();
        var courses = await (from assignment in db.PathwayLevelCourses.AsNoTracking()
                             join course in db.Courses.AsNoTracking() on assignment.CourseId equals course.Id
                             where levelIds.Contains(assignment.PathwayLevelId)
                                   && (!publishedOnly || course.Status == ContentStatus.Published)
                             orderby assignment.Order
                             select new { assignment.PathwayLevelId, Dto = new PathwayLevelCourseDto(
                                 assignment.Id, course.Id, course.Title, course.Slug, course.Difficulty,
                                 course.EstimatedDurationMinutes, assignment.Order, assignment.IsRequired) })
            .ToListAsync(cancellationToken);
        var levelDtos = levels.Select(level => new PathwayLevelDto(level.Id, level.Name, level.Description, level.Order,
            courses.Where(x => x.PathwayLevelId == level.Id).Select(x => x.Dto).ToList())).ToList();
        return new CareerPathwayDto(pathway.Id, pathway.CareerId, pathway.Name, pathway.Description,
            pathway.Version, pathway.IsPrimary, pathway.Status, levelDtos);
    }
}
