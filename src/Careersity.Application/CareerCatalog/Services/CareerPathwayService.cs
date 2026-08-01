using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.CareerCatalog.Dtos;
using Careersity.Application.CareerCatalog.Requests;
using Careersity.Application.Common.Exceptions;
using Careersity.Domain.Careers;
using Careersity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.CareerCatalog.Services;

public sealed class CareerPathwayService(ICareersityDbContext db) : ICareerPathwayService
{
    public async Task<CareerPathwayDto> CreateAsync(Guid careerId, CreateCareerPathwayRequest request, CancellationToken cancellationToken)
    {
        if (careerId != request.CareerId) throw new RequestValidationException("Route career ID must match the request career ID.");
        await RequireCareerAsync(careerId, cancellationToken);
        await EnsureUniqueVersionAsync(careerId, request.Version.Trim(), null, cancellationToken);
        var pathway = new CareerPathway(careerId, request.Name, request.Version, request.Description, request.IsPrimary);
        if (request.IsPrimary) await DemoteExistingPrimaryAsync(careerId, null, cancellationToken);
        db.CareerPathways.Add(pathway); await db.SaveChangesAsync(cancellationToken);
        return await CareerCatalogProjections.LoadPathwayAsync(db, pathway, false, cancellationToken);
    }

    public async Task<CareerPathwayDto> UpdateAsync(Guid careerId, Guid pathwayId, UpdateCareerPathwayRequest request, CancellationToken cancellationToken)
    {
        var pathway = await LoadAggregateAsync(careerId, pathwayId, cancellationToken); EnsureMutable(pathway);
        await EnsureUniqueVersionAsync(careerId, request.Version.Trim(), pathwayId, cancellationToken);
        if (request.IsPrimary && !pathway.IsPrimary) await DemoteExistingPrimaryAsync(careerId, pathwayId, cancellationToken);
        pathway.UpdateDetails(request.Name, request.Version, request.Description); pathway.SetPrimary(request.IsPrimary);
        await db.SaveChangesAsync(cancellationToken);
        return await CareerCatalogProjections.LoadPathwayAsync(db, pathway, false, cancellationToken);
    }

    public async Task PublishAsync(Guid careerId, Guid pathwayId, CancellationToken cancellationToken)
    {
        var pathway = await LoadAggregateAsync(careerId, pathwayId, cancellationToken);
        var career = await db.Careers.AsNoTracking().SingleAsync(x => x.Id == careerId, cancellationToken);
        if (career.Status != ContentStatus.Published) throw new ConflictException("The parent career must be published first.");
        if (pathway.Levels.Count == 0) throw new ConflictException("A pathway must contain at least one level.");
        if (pathway.Levels.Any(x => x.Courses.Count == 0)) throw new ConflictException("Every pathway level must contain at least one course.");
        var courseIds = pathway.Levels.SelectMany(x => x.Courses).Select(x => x.CourseId).Distinct().ToArray();
        var publishedCount = await db.Courses.CountAsync(x => courseIds.Contains(x.Id) && x.Status == ContentStatus.Published, cancellationToken);
        if (publishedCount != courseIds.Length) throw new ConflictException("Every pathway course must be published before the pathway can be published.");
        pathway.Publish(); await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveAsync(Guid careerId, Guid pathwayId, CancellationToken cancellationToken)
    { var pathway = await LoadAggregateAsync(careerId, pathwayId, cancellationToken); pathway.Archive(); await db.SaveChangesAsync(cancellationToken); }

    public async Task<CareerPathwayDto> GetAdminAsync(Guid careerId, Guid pathwayId, CancellationToken cancellationToken) =>
        await CareerCatalogProjections.LoadPathwayAsync(db, await FindAsync(careerId, pathwayId, true, cancellationToken), false, cancellationToken);

    public async Task<IReadOnlyCollection<CareerPathwayDto>> ListAdminAsync(Guid careerId, CancellationToken cancellationToken)
    {
        await RequireCareerAsync(careerId, cancellationToken);
        var pathways = await db.CareerPathways.AsNoTracking().Where(x => x.CareerId == careerId).OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Version).ToListAsync(cancellationToken);
        var result = new List<CareerPathwayDto>();
        foreach (var pathway in pathways) result.Add(await CareerCatalogProjections.LoadPathwayAsync(db, pathway, false, cancellationToken));
        return result;
    }

    public async Task<CareerPathwayDto> GetPublicPrimaryAsync(Guid careerId, CancellationToken cancellationToken)
    {
        if (!await db.Careers.AnyAsync(x => x.Id == careerId && x.Status == ContentStatus.Published, cancellationToken))
            throw new NotFoundException("Published career was not found.");
        var pathway = await db.CareerPathways.AsNoTracking().SingleOrDefaultAsync(
            x => x.CareerId == careerId && x.IsPrimary && x.Status == ContentStatus.Published, cancellationToken)
            ?? throw new NotFoundException("Published primary pathway was not found.");
        return await CareerCatalogProjections.LoadPathwayAsync(db, pathway, true, cancellationToken);
    }

    public async Task<PathwayLevelDto> AddLevelAsync(Guid careerId, Guid pathwayId, AddPathwayLevelRequest request, CancellationToken cancellationToken)
    {
        var pathway = await LoadAggregateAsync(careerId, pathwayId, cancellationToken); EnsureMutable(pathway);
        if (pathway.Levels.Any(x => x.Order == request.Order)) throw new ConflictException("Pathway level order must be unique.");
        var level = new PathwayLevel(pathway.Id, request.Name, request.Order, request.Description); pathway.AddLevel(level);
        db.PathwayLevels.Add(level);
        await db.SaveChangesAsync(cancellationToken);
        return new PathwayLevelDto(level.Id, level.Name, level.Description, level.Order, []);
    }

    public async Task<PathwayLevelDto> UpdateLevelAsync(Guid careerId, Guid pathwayId, Guid levelId, UpdatePathwayLevelRequest request, CancellationToken cancellationToken)
    {
        var pathway = await LoadAggregateAsync(careerId, pathwayId, cancellationToken); EnsureMutable(pathway);
        var level = pathway.Levels.SingleOrDefault(x => x.Id == levelId) ?? throw new NotFoundException("Pathway level was not found.");
        if (pathway.Levels.Any(x => x.Id != levelId && x.Order == request.Order)) throw new ConflictException("Pathway level order must be unique.");
        level.UpdateDetails(request.Name, request.Description); pathway.ReorderLevel(levelId, request.Order);
        await db.SaveChangesAsync(cancellationToken);
        return (await CareerCatalogProjections.LoadPathwayAsync(db, pathway, false, cancellationToken)).Levels.Single(x => x.Id == levelId);
    }

    public async Task RemoveLevelAsync(Guid careerId, Guid pathwayId, Guid levelId, CancellationToken cancellationToken)
    { var pathway = await LoadAggregateAsync(careerId, pathwayId, cancellationToken); EnsureMutable(pathway); pathway.RemoveLevel(levelId); await db.SaveChangesAsync(cancellationToken); }

    public async Task ReorderLevelsAsync(Guid careerId, Guid pathwayId, ReorderPathwayLevelsRequest request, CancellationToken cancellationToken)
    {
        var pathway = await LoadAggregateAsync(careerId, pathwayId, cancellationToken); EnsureMutable(pathway);
        ValidateCompleteOrder(pathway.Levels.Select(x => x.Id), request.Levels.Select(x => (x.LevelId, x.Order)), "levels");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var temporaryStart = pathway.Levels.Max(x => x.Order) + pathway.Levels.Count + 1;
        var index = 0; foreach (var level in pathway.Levels.ToList()) pathway.ReorderLevel(level.Id, temporaryStart + index++);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var item in request.Levels.OrderBy(x => x.Order)) pathway.ReorderLevel(item.LevelId, item.Order);
        await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }

    public async Task<PathwayLevelCourseDto> AddCourseAsync(Guid careerId, Guid pathwayId, Guid levelId, AddPathwayLevelCourseRequest request, CancellationToken cancellationToken)
    {
        var pathway = await LoadAggregateAsync(careerId, pathwayId, cancellationToken); EnsureMutable(pathway);
        var level = GetLevel(pathway, levelId);
        var course = await db.Courses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken)
            ?? throw new NotFoundException("Course was not found.");
        if (level.Courses.Any(x => x.CourseId == request.CourseId)) throw new ConflictException("The course is already assigned to this level.");
        if (level.Courses.Any(x => x.Order == request.Order)) throw new ConflictException("Course order must be unique within the level.");
        var assignment = level.AddCourse(request.CourseId, request.Order, request.IsRequired);
        db.PathwayLevelCourses.Add(assignment);
        await db.SaveChangesAsync(cancellationToken); return ToCourseDto(assignment, course);
    }

    public async Task<PathwayLevelCourseDto> UpdateCourseAsync(Guid careerId, Guid pathwayId, Guid levelId, Guid assignmentId, UpdatePathwayLevelCourseRequest request, CancellationToken cancellationToken)
    {
        var pathway = await LoadAggregateAsync(careerId, pathwayId, cancellationToken); EnsureMutable(pathway); var level = GetLevel(pathway, levelId);
        var assignment = level.Courses.SingleOrDefault(x => x.Id == assignmentId) ?? throw new NotFoundException("Pathway course assignment was not found.");
        if (level.Courses.Any(x => x.Id != assignmentId && x.Order == request.Order)) throw new ConflictException("Course order must be unique within the level.");
        level.ReorderCourse(assignment.CourseId, request.Order); assignment.SetRequired(request.IsRequired); await db.SaveChangesAsync(cancellationToken);
        var course = await db.Courses.AsNoTracking().SingleAsync(x => x.Id == assignment.CourseId, cancellationToken); return ToCourseDto(assignment, course);
    }

    public async Task RemoveCourseAsync(Guid careerId, Guid pathwayId, Guid levelId, Guid assignmentId, CancellationToken cancellationToken)
    {
        var pathway = await LoadAggregateAsync(careerId, pathwayId, cancellationToken); EnsureMutable(pathway); var level = GetLevel(pathway, levelId);
        var assignment = level.Courses.SingleOrDefault(x => x.Id == assignmentId) ?? throw new NotFoundException("Pathway course assignment was not found.");
        level.RemoveCourse(assignment.CourseId); await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReorderCoursesAsync(Guid careerId, Guid pathwayId, Guid levelId, ReorderPathwayCoursesRequest request, CancellationToken cancellationToken)
    {
        var pathway = await LoadAggregateAsync(careerId, pathwayId, cancellationToken); EnsureMutable(pathway); var level = GetLevel(pathway, levelId);
        ValidateCompleteOrder(level.Courses.Select(x => x.Id), request.Courses.Select(x => (x.AssignmentId, x.Order)), "courses");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var temporaryStart = level.Courses.Max(x => x.Order) + level.Courses.Count + 1;
        var index = 0; foreach (var assignment in level.Courses.ToList()) level.ReorderCourse(assignment.CourseId, temporaryStart + index++);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var item in request.Courses.OrderBy(x => x.Order))
        { var assignment = level.Courses.Single(x => x.Id == item.AssignmentId); level.ReorderCourse(assignment.CourseId, item.Order); }
        await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }

    private async Task<CareerPathway> LoadAggregateAsync(Guid careerId, Guid pathwayId, CancellationToken cancellationToken) =>
        await db.CareerPathways.Include(x => x.Levels).ThenInclude(x => x.Courses)
            .SingleOrDefaultAsync(x => x.Id == pathwayId && x.CareerId == careerId, cancellationToken)
        ?? throw new NotFoundException("Career pathway was not found.");
    private async Task<CareerPathway> FindAsync(Guid careerId, Guid pathwayId, bool noTracking, CancellationToken cancellationToken)
    { var query = noTracking ? db.CareerPathways.AsNoTracking() : db.CareerPathways; return await query.SingleOrDefaultAsync(x => x.Id == pathwayId && x.CareerId == careerId, cancellationToken) ?? throw new NotFoundException("Career pathway was not found."); }
    private async Task RequireCareerAsync(Guid careerId, CancellationToken cancellationToken)
    { if (!await db.Careers.AnyAsync(x => x.Id == careerId, cancellationToken)) throw new NotFoundException("Career was not found."); }
    private async Task EnsureUniqueVersionAsync(Guid careerId, string version, Guid? excludedId, CancellationToken cancellationToken)
    { if (await db.CareerPathways.AnyAsync(x => x.CareerId == careerId && x.Version == version && (!excludedId.HasValue || x.Id != excludedId), cancellationToken)) throw new ConflictException("A pathway with this version already exists for the career."); }
    private async Task DemoteExistingPrimaryAsync(Guid careerId, Guid? excludedId, CancellationToken cancellationToken)
    { var current = await db.CareerPathways.SingleOrDefaultAsync(x => x.CareerId == careerId && x.IsPrimary && (!excludedId.HasValue || x.Id != excludedId), cancellationToken); current?.SetPrimary(false); }
    private static void EnsureMutable(CareerPathway pathway) { if (pathway.Status != ContentStatus.Draft) throw new ConflictException("Published or archived pathway structure and metadata are immutable."); }
    private static PathwayLevel GetLevel(CareerPathway pathway, Guid levelId) => pathway.Levels.SingleOrDefault(x => x.Id == levelId) ?? throw new NotFoundException("Pathway level was not found.");
    private static void ValidateCompleteOrder(IEnumerable<Guid> actualIds, IEnumerable<(Guid Id, int Order)> requested, string label)
    {
        var actual = actualIds.ToArray(); var items = requested.ToArray();
        if (items.Length != actual.Length || items.Select(x => x.Id).Distinct().Count() != items.Length || items.Any(x => !actual.Contains(x.Id))) throw new RequestValidationException($"The reorder request must contain every {label} item exactly once.");
        if (items.Any(x => x.Order < 0) || items.Select(x => x.Order).Distinct().Count() != items.Length || !items.Select(x => x.Order).Order().SequenceEqual(Enumerable.Range(0, items.Length))) throw new RequestValidationException("Orders must be unique, nonnegative, and contiguous from zero.");
    }
    private static PathwayLevelCourseDto ToCourseDto(PathwayLevelCourse assignment, Domain.Courses.Course course) => new(assignment.Id, course.Id, course.Title, course.Slug, course.Difficulty, course.EstimatedDurationMinutes, assignment.Order, assignment.IsRequired);
}
