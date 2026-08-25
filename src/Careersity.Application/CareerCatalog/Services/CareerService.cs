using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.CareerCatalog.Dtos;
using Careersity.Application.CareerCatalog.Requests;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.Common.Models;
using Careersity.Domain.Careers;
using Careersity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.CareerCatalog.Services;

public sealed class CareerService(ICareersityDbContext db, CareerReadinessEvaluator? readiness = null) : ICareerService
{
    public async Task<CareerDetailDto> CreateAsync(CreateCareerRequest request, CancellationToken cancellationToken)
    {
        await RequireCategoryAsync(request.CareerCategoryId, cancellationToken);
        var slug = request.Slug.Trim().ToLowerInvariant();
        await EnsureUniqueSlugAsync(slug, null, cancellationToken);
        var career = new Career(request.CareerCategoryId, request.Title, slug, request.ShortDescription,
            request.DetailedDescription, request.Responsibilities, request.EstimatedDurationWeeks);
        db.Careers.Add(career); await db.SaveChangesAsync(cancellationToken);
        return await BuildDetailAsync(career, false, cancellationToken);
    }

    public async Task<CareerDetailDto> UpdateAsync(Guid id, UpdateCareerRequest request, CancellationToken cancellationToken)
    {
        var career = await FindAsync(id, cancellationToken);
        if (career.Status != ContentStatus.Draft) throw new ConflictException("Published or archived career details are immutable.");
        var slug = request.Slug.Trim().ToLowerInvariant();
        await EnsureUniqueSlugAsync(slug, id, cancellationToken);
        career.UpdateBasicDetails(request.Title, slug, request.ShortDescription, request.DetailedDescription,
            request.Responsibilities, request.EstimatedDurationWeeks);
        await db.SaveChangesAsync(cancellationToken);
        return await BuildDetailAsync(career, false, cancellationToken);
    }

    public async Task ChangeCategoryAsync(Guid id, ChangeCareerCategoryRequest request, CancellationToken cancellationToken)
    {
        var career = await FindAsync(id, cancellationToken);
        if (career.Status != ContentStatus.Draft) throw new ConflictException("Published or archived careers cannot change category.");
        await RequireCategoryAsync(request.CareerCategoryId, cancellationToken);
        career.ChangeCategory(request.CareerCategoryId); await db.SaveChangesAsync(cancellationToken);
    }

    public async Task PublishAsync(Guid id, CancellationToken cancellationToken)
    {
        var career = await FindAsync(id, cancellationToken);
        if (career.Status == ContentStatus.Published) return;
        var result = await GetReadinessAsync(id, cancellationToken);
        if (!result.IsReady)
            throw new ConflictException("Career curriculum is not ready to publish: " +
                string.Join("; ", result.Checks.Where(x => x.Blocking && !x.Passed).Select(x => x.Message)));
        career.Publish(); await db.SaveChangesAsync(cancellationToken);
    }

    public Task<CareerReadinessDto> GetReadinessAsync(Guid id, CancellationToken cancellationToken) =>
        (readiness ?? new CareerReadinessEvaluator(db)).EvaluateAsync(id, cancellationToken);

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
    { var career = await FindAsync(id, cancellationToken); career.Archive(); await db.SaveChangesAsync(cancellationToken); }

    public async Task<CareerDetailDto> GetAdminAsync(Guid id, CancellationToken cancellationToken) =>
        await BuildDetailAsync(await FindAsync(id, cancellationToken), false, cancellationToken);

    public Task<PagedResult<CareerListItemDto>> ListAdminAsync(CareerQuery query, CancellationToken cancellationToken) =>
        ListAsync(query, false, cancellationToken);

    public Task<PagedResult<CareerListItemDto>> ListPublishedAsync(CareerQuery query, CancellationToken cancellationToken) =>
        ListAsync(query with { Status = ContentStatus.Published }, true, cancellationToken);

    public async Task<CareerDetailDto> GetPublishedBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var normalized = slug.Trim().ToLowerInvariant();
        var career = await db.Careers.AsNoTracking().SingleOrDefaultAsync(
            x => x.Slug == normalized && x.Status == ContentStatus.Published, cancellationToken)
            ?? throw new NotFoundException("Published career was not found.");
        return await BuildDetailAsync(career, true, cancellationToken);
    }

    private async Task<PagedResult<CareerListItemDto>> ListAsync(CareerQuery request, bool publicOnly, CancellationToken cancellationToken)
    {
        var query = from career in db.Careers.AsNoTracking()
                    join category in db.CareerCategories.AsNoTracking() on career.CareerCategoryId equals category.Id
                    select new { career, category };
        if (publicOnly) query = query.Where(x => x.career.Status == ContentStatus.Published && x.category.Status == ContentStatus.Published);
        else if (request.Status.HasValue) query = query.Where(x => x.career.Status == request.Status.Value);
        if (request.CategoryId.HasValue) query = query.Where(x => x.career.CareerCategoryId == request.CategoryId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x => x.career.Title.ToLower().Contains(search) || x.career.ShortDescription.ToLower().Contains(search));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.career.Title).Skip((request.ValidatedPage - 1) * request.ValidatedPageSize)
            .Take(request.ValidatedPageSize).Select(x => new CareerListItemDto(x.career.Id, x.career.CareerCategoryId,
                x.category.Name, x.career.Title, x.career.Slug, x.career.ShortDescription,
                x.career.EstimatedDurationWeeks, x.career.Status)).ToListAsync(cancellationToken);
        return new PagedResult<CareerListItemDto>(items, request.ValidatedPage, request.ValidatedPageSize, total);
    }

    private async Task<CareerDetailDto> BuildDetailAsync(Career career, bool publicOnly, CancellationToken cancellationToken)
    {
        var category = await db.CareerCategories.AsNoTracking().SingleAsync(x => x.Id == career.CareerCategoryId, cancellationToken);
        if (publicOnly && category.Status != ContentStatus.Published) throw new NotFoundException("Published career was not found.");
        var skills = await CareerCatalogProjections.LoadSkillsAsync(db, career.Id, publicOnly, cancellationToken);
        var pathwayQuery = db.CareerPathways.AsNoTracking().Where(x => x.CareerId == career.Id && x.IsPrimary);
        if (publicOnly) pathwayQuery = pathwayQuery.Where(x => x.Status == ContentStatus.Published);
        var pathway = await pathwayQuery.SingleOrDefaultAsync(cancellationToken);
        var pathwayDto = pathway is null ? null : await CareerCatalogProjections.LoadPathwayAsync(db, pathway, publicOnly, cancellationToken);
        return new CareerDetailDto(career.Id, CareerCatalogProjections.ToDto(category), career.Title, career.Slug,
            career.ShortDescription, career.DetailedDescription, career.Responsibilities, career.EstimatedDurationWeeks,
            career.Status, skills, pathwayDto, career.CreatedAtUtc, career.UpdatedAtUtc);
    }

    private async Task EnsureUniqueSlugAsync(string slug, Guid? excludedId, CancellationToken cancellationToken)
    {
        if (await db.Careers.AnyAsync(x => x.Slug == slug && (!excludedId.HasValue || x.Id != excludedId.Value), cancellationToken))
            throw new ConflictException("A career with this slug already exists.");
    }

    private async Task<CareerCategory> RequireCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        await db.CareerCategories.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Career category was not found.");

    private async Task<Career> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Careers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Career was not found.");
}
