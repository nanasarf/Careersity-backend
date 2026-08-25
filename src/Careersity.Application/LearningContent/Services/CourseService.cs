using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.Common.Models;
using Careersity.Application.LearningContent.Dtos;
using Careersity.Application.LearningContent.Requests;
using Careersity.Domain.Courses;
using Careersity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.LearningContent.Services;

public sealed class CourseService(ICareersityDbContext db) : ICourseService
{
    public async Task<CourseDetailDto> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken)
    {
        var slug = SkillService.Normalize(request.Slug); await EnsureUniqueAsync(slug, null, cancellationToken);
        var course = new Course(request.Title, slug, request.ShortDescription, request.Difficulty,
            request.EstimatedDurationMinutes, request.DetailedDescription);
        db.Courses.Add(course); await db.SaveChangesAsync(cancellationToken); return await BuildDetailAsync(course, false, cancellationToken);
    }
    public async Task<CourseDetailDto> UpdateAsync(Guid id, UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        var course = await FindAsync(id, false, cancellationToken); SkillService.RequireDraft(course.Status, "course");
        var slug = SkillService.Normalize(request.Slug); await EnsureUniqueAsync(slug, id, cancellationToken);
        course.UpdateDetails(request.Title, slug, request.ShortDescription, request.DetailedDescription);
        course.ChangeDifficulty(request.Difficulty); course.ChangeDuration(request.EstimatedDurationMinutes);
        await db.SaveChangesAsync(cancellationToken); return await BuildDetailAsync(course, false, cancellationToken);
    }
    public async Task PublishAsync(Guid id, CancellationToken cancellationToken)
    {
        var course = await FindAsync(id, true, cancellationToken); SkillService.RequireDraft(course.Status, "course");
        if (!course.Lessons.Any(x => x.IsRequired)) throw new ConflictException("A course requires at least one required lesson before publication.");
        if (course.CourseSkills.Count == 0) throw new ConflictException("A course requires at least one skill before publication.");
        if (!course.CourseSkills.Any(x => x.IsPrimary)) throw new ConflictException("A course requires at least one primary skill before publication.");
        var skillIds = course.CourseSkills.Select(x => x.SkillId).ToArray();
        if (await db.Skills.CountAsync(x => skillIds.Contains(x.Id) && x.Status == ContentStatus.Published, cancellationToken) != skillIds.Length)
            throw new ConflictException("Every associated skill must be published before the course.");
        var prerequisiteIds = course.Prerequisites.Select(x => x.PrerequisiteCourseId).ToArray();
        if (await db.Courses.CountAsync(x => prerequisiteIds.Contains(x.Id) && x.Status == ContentStatus.Published, cancellationToken) != prerequisiteIds.Length)
            throw new ConflictException("Every prerequisite course must be published before the course.");
        var assignedResourceIds = await db.CourseExternalResources.AsNoTracking().Where(x => x.CourseId == id).Select(x => x.ExternalLearningResourceId).ToListAsync(cancellationToken);
        if (await db.ExternalLearningResources.AsNoTracking().CountAsync(x => assignedResourceIds.Contains(x.Id) && x.Status == ContentStatus.Published, cancellationToken) != assignedResourceIds.Count)
            throw new ConflictException("Every assigned external resource must be published before the course.");
        if (course.Lessons.Select(x => x.Order).Distinct().Count() != course.Lessons.Count ||
            course.Lessons.Select(x => x.Slug).Distinct(StringComparer.OrdinalIgnoreCase).Count() != course.Lessons.Count)
            throw new ConflictException("Lesson order and slug must be unique within the course.");
        course.Publish(); await db.SaveChangesAsync(cancellationToken);
    }
    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
    {
        var course = await FindAsync(id, false, cancellationToken);
        var usedByPublishedCareer = await (from assignment in db.PathwayLevelCourses.AsNoTracking()
            join level in db.PathwayLevels.AsNoTracking() on assignment.PathwayLevelId equals level.Id
            join pathway in db.CareerPathways.AsNoTracking() on level.CareerPathwayId equals pathway.Id
            join career in db.Careers.AsNoTracking() on pathway.CareerId equals career.Id
            where assignment.CourseId == id && pathway.IsPrimary && pathway.Status == ContentStatus.Published && career.Status == ContentStatus.Published
            select assignment.Id).AnyAsync(cancellationToken);
        if (usedByPublishedCareer) throw new ConflictException("A course used by a published career curriculum cannot be archived.");
        course.Archive(); await db.SaveChangesAsync(cancellationToken);
    }
    public async Task<CourseDetailDto> GetAdminAsync(Guid id, CancellationToken cancellationToken) => await BuildDetailAsync(await FindAsync(id, false, cancellationToken), false, cancellationToken);
    public Task<PagedResult<CourseListItemDto>> ListAdminAsync(CourseQuery query, CancellationToken cancellationToken) => ListAsync(query, false, cancellationToken);
    public Task<PagedResult<CourseListItemDto>> ListPublishedAsync(CourseQuery query, CancellationToken cancellationToken) => ListAsync(query, true, cancellationToken);
    public async Task<CourseDetailDto> GetPublishedAsync(string slug, CancellationToken cancellationToken)
    {
        var normalized = SkillService.Normalize(slug);
        var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == normalized && x.Status == ContentStatus.Published, cancellationToken)
            ?? throw new NotFoundException("Published course was not found."); return await BuildDetailAsync(course, true, cancellationToken);
    }
    private async Task<PagedResult<CourseListItemDto>> ListAsync(CourseQuery request, bool publicOnly, CancellationToken cancellationToken)
    {
        var query = db.Courses.AsNoTracking();
        if (publicOnly) query = query.Where(x => x.Status == ContentStatus.Published); else if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        if (request.Difficulty.HasValue) query = query.Where(x => x.Difficulty == request.Difficulty);
        if (request.SkillId.HasValue) query = query.Where(x => x.CourseSkills.Any(s => s.SkillId == request.SkillId));
        if (!string.IsNullOrWhiteSpace(request.Search)) { var search = request.Search.Trim().ToLowerInvariant(); query = query.Where(x => x.Title.ToLower().Contains(search) || x.ShortDescription.ToLower().Contains(search)); }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Title).Skip((request.ValidatedPage - 1) * request.ValidatedPageSize).Take(request.ValidatedPageSize)
            .Select(x => new CourseListItemDto(x.Id, x.Title, x.Slug, x.ShortDescription, x.Difficulty,
                x.EstimatedDurationMinutes, x.Status, x.Lessons.Count, x.CourseSkills.Count)).ToListAsync(cancellationToken);
        return new(items, request.ValidatedPage, request.ValidatedPageSize, total);
    }
    internal async Task<CourseDetailDto> BuildDetailAsync(Course course, bool publicOnly, CancellationToken cancellationToken)
    {
        var lessons = await db.Lessons.AsNoTracking().Where(x => x.CourseId == course.Id).OrderBy(x => x.Order)
            .Select(x => new LessonSummaryDto(x.Id, x.Title, x.Slug, x.Summary, x.ContentType, x.EstimatedDurationMinutes, x.Order, x.IsRequired)).ToListAsync(cancellationToken);
        var prerequisites = await (from relation in db.CoursePrerequisites.AsNoTracking()
            join prerequisite in db.Courses.AsNoTracking() on relation.PrerequisiteCourseId equals prerequisite.Id
            where relation.CourseId == course.Id && (!publicOnly || prerequisite.Status == ContentStatus.Published)
            orderby prerequisite.Title
            select new CoursePrerequisiteDto(relation.Id, prerequisite.Id, prerequisite.Title, prerequisite.Slug, relation.IsRequired)).ToListAsync(cancellationToken);
        var skills = await (from relation in db.CourseSkills.AsNoTracking()
            join skill in db.Skills.AsNoTracking() on relation.SkillId equals skill.Id
            where relation.CourseId == course.Id && (!publicOnly || skill.Status == ContentStatus.Published)
            orderby skill.Name
            select new CourseSkillDto(relation.Id, skill.Id, skill.Name, skill.Slug, skill.Category, relation.ProficiencyLevel, relation.IsPrimary)).ToListAsync(cancellationToken);
        return new(course.Id, course.Title, course.Slug, course.ShortDescription, course.DetailedDescription, course.Difficulty,
            course.EstimatedDurationMinutes, course.Status, lessons, prerequisites, skills, course.CreatedAtUtc, course.UpdatedAtUtc);
    }
    internal async Task<Course> FindAsync(Guid id, bool includeChildren, CancellationToken token)
    {
        IQueryable<Course> query = db.Courses;
        if (includeChildren) query = query.Include(x => x.Lessons).Include(x => x.Prerequisites).Include(x => x.CourseSkills);
        return await query.SingleOrDefaultAsync(x => x.Id == id, token) ?? throw new NotFoundException("Course was not found.");
    }
    private async Task EnsureUniqueAsync(string slug, Guid? excluded, CancellationToken token)
    { if (await db.Courses.AnyAsync(x => x.Slug == slug && (!excluded.HasValue || x.Id != excluded), token)) throw new ConflictException("A course with this slug already exists."); }
}
