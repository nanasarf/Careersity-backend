using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.CareerCatalog.Dtos;
using Careersity.Application.CareerCatalog.Requests;
using Careersity.Application.Common.Exceptions;
using Careersity.Domain.Careers;
using Careersity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.CareerCatalog.Services;

public sealed class CareerCategoryService(ICareersityDbContext db) : ICareerCategoryService
{
    public async Task<CareerCategoryDto> CreateAsync(CreateCareerCategoryRequest request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await db.CareerCategories.AnyAsync(x => x.Slug == slug, cancellationToken))
            throw new ConflictException("A career category with this slug already exists.");
        var category = new CareerCategory(request.Name, slug, request.Description);
        db.CareerCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return CareerCatalogProjections.ToDto(category);
    }

    public async Task<CareerCategoryDto> UpdateAsync(Guid id, UpdateCareerCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await FindAsync(id, cancellationToken);
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await db.CareerCategories.AnyAsync(x => x.Slug == slug && x.Id != id, cancellationToken))
            throw new ConflictException("A career category with this slug already exists.");
        category.UpdateDetails(request.Name, slug, request.Description);
        await db.SaveChangesAsync(cancellationToken);
        return CareerCatalogProjections.ToDto(category);
    }

    public async Task PublishAsync(Guid id, CancellationToken cancellationToken)
    { var category = await FindAsync(id, cancellationToken); category.Publish(); await db.SaveChangesAsync(cancellationToken); }

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
    { var category = await FindAsync(id, cancellationToken); category.Archive(); await db.SaveChangesAsync(cancellationToken); }

    public async Task<CareerCategoryDto> GetAdminAsync(Guid id, CancellationToken cancellationToken) =>
        CareerCatalogProjections.ToDto(await FindAsync(id, cancellationToken));

    public async Task<IReadOnlyCollection<CareerCategoryDto>> ListAdminAsync(CancellationToken cancellationToken) =>
        await db.CareerCategories.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new CareerCategoryDto(x.Id, x.Name, x.Slug, x.Description, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<CareerCategoryDto>> ListPublishedAsync(CancellationToken cancellationToken) =>
        await db.CareerCategories.AsNoTracking().Where(x => x.Status == ContentStatus.Published).OrderBy(x => x.Name)
            .Select(x => new CareerCategoryDto(x.Id, x.Name, x.Slug, x.Description, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

    private async Task<CareerCategory> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await db.CareerCategories.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
        ?? throw new NotFoundException("Career category was not found.");
}
