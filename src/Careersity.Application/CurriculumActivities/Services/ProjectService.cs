using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.Common.Models;
using Careersity.Application.CurriculumActivities.Dtos;
using Careersity.Application.CurriculumActivities.Requests;
using Careersity.Domain.Enums;
using Careersity.Domain.Projects;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.CurriculumActivities.Services;

public sealed class ProjectService(ICareersityDbContext db) : IProjectService
{
    public async Task<ProjectAdminDetailDto> CreateAsync(CreateProjectRequest request, CancellationToken token)
    {
        var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.CourseId, token) ?? throw new NotFoundException("Course was not found.");
        var project = new Project(course.Id, request.Title, request.Description, request.Instructions, request.SubmissionType,
            request.EstimatedDurationMinutes, request.ExpectedOutput, request.EvaluationCriteria);
        db.Projects.Add(project); await db.SaveChangesAsync(token); return ToAdmin(project, course.Title);
    }
    public async Task<ProjectAdminDetailDto> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken token)
    {
        var project = await FindAsync(id, token); RequireDraft(project.Status);
        project.UpdateDetails(request.Title, request.Description, request.Instructions, request.ExpectedOutput, request.EvaluationCriteria);
        project.ChangeSubmissionType(request.SubmissionType); project.ChangeDuration(request.EstimatedDurationMinutes);
        await db.SaveChangesAsync(token); return await GetAdminAsync(id, token);
    }
    public async Task PublishAsync(Guid id, CancellationToken token)
    {
        var project = await FindAsync(id, token); RequireDraft(project.Status);
        var publishedCourse = await db.Courses.AsNoTracking().AnyAsync(x => x.Id == project.CourseId && x.Status == ContentStatus.Published, token);
        if (!publishedCourse) throw new ConflictException("The parent course must be published before the project.");
        project.Publish(); await db.SaveChangesAsync(token);
    }
    public async Task ArchiveAsync(Guid id, CancellationToken token)
    { var project = await FindAsync(id, token); project.Archive(); await db.SaveChangesAsync(token); }
    public async Task<ProjectAdminDetailDto> GetAdminAsync(Guid id, CancellationToken token)
    {
        var result = await (from project in db.Projects.AsNoTracking() join course in db.Courses.AsNoTracking() on project.CourseId equals course.Id
            where project.Id == id select new { project, course.Title }).SingleOrDefaultAsync(token) ?? throw new NotFoundException("Project was not found.");
        return ToAdmin(result.project, result.Title);
    }
    public async Task<PagedResult<ProjectListItemDto>> ListAdminAsync(ProjectQuery request, CancellationToken token)
    {
        var query = from project in db.Projects.AsNoTracking() join course in db.Courses.AsNoTracking() on project.CourseId equals course.Id select new { project, course };
        if (request.CourseId.HasValue) query = query.Where(x => x.project.CourseId == request.CourseId); if (request.Status.HasValue) query = query.Where(x => x.project.Status == request.Status);
        if (request.SubmissionType.HasValue) query = query.Where(x => x.project.SubmissionType == request.SubmissionType);
        if (!string.IsNullOrWhiteSpace(request.Search)) { var search = request.Search.Trim().ToLowerInvariant(); query = query.Where(x => x.project.Title.ToLower().Contains(search) || x.project.Description.ToLower().Contains(search) || x.project.Instructions.ToLower().Contains(search)); }
        var total = await query.CountAsync(token); var items = await query.OrderBy(x => x.project.Title).Skip((request.ValidatedPage - 1) * request.ValidatedPageSize).Take(request.ValidatedPageSize)
            .Select(x => new ProjectListItemDto(x.project.Id, x.course.Id, x.course.Title, x.project.Title, x.project.Description,
                x.project.SubmissionType, x.project.EstimatedDurationMinutes, x.project.Status)).ToListAsync(token); return new(items, request.ValidatedPage, request.ValidatedPageSize, total);
    }
    public async Task<IReadOnlyCollection<PublicProjectSummaryDto>> ListPublishedAsync(string courseSlug, CancellationToken token)
    {
        var slug = courseSlug.Trim().ToLowerInvariant(); var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug && x.Status == ContentStatus.Published, token) ?? throw new NotFoundException("Published course was not found.");
        return await db.Projects.AsNoTracking().Where(x => x.CourseId == course.Id && x.Status == ContentStatus.Published).OrderBy(x => x.Title)
            .Select(x => new PublicProjectSummaryDto(x.Id, x.Title, x.Description, x.SubmissionType, x.EstimatedDurationMinutes)).ToListAsync(token);
    }
    public async Task<PublicProjectDetailDto> GetPublishedAsync(string courseSlug, Guid projectId, CancellationToken token)
    {
        var slug = courseSlug.Trim().ToLowerInvariant(); return await (from project in db.Projects.AsNoTracking() join course in db.Courses.AsNoTracking() on project.CourseId equals course.Id
            where project.Id == projectId && project.Status == ContentStatus.Published && course.Slug == slug && course.Status == ContentStatus.Published
            select new PublicProjectDetailDto(project.Id, course.Id, course.Title, course.Slug, project.Title, project.Description,
                project.Instructions, project.ExpectedOutput, project.EvaluationCriteria, project.SubmissionType, project.EstimatedDurationMinutes))
            .SingleOrDefaultAsync(token) ?? throw new NotFoundException("Published project was not found.");
    }
    private async Task<Project> FindAsync(Guid id, CancellationToken token) => await db.Projects.SingleOrDefaultAsync(x => x.Id == id, token) ?? throw new NotFoundException("Project was not found.");
    private static void RequireDraft(ContentStatus status) { if (status != ContentStatus.Draft) throw new ConflictException("The project is immutable after publication or archiving."); }
    private static ProjectAdminDetailDto ToAdmin(Project x, string courseTitle) => new(x.Id, x.CourseId, courseTitle, x.Title, x.Description,
        x.Instructions, x.ExpectedOutput, x.EvaluationCriteria, x.SubmissionType, x.EstimatedDurationMinutes, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc);
}
