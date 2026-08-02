using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.Common.Models;
using Careersity.Application.LearningContent.Dtos;
using Careersity.Application.LearningContent.Requests;
using Careersity.Domain.Enums;
using Careersity.Domain.Skills;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.LearningContent.Services;

public sealed class SkillService(ICareersityDbContext db) : ISkillService
{
    public async Task<SkillDto> CreateAsync(CreateSkillRequest request, CancellationToken cancellationToken)
    {
        var slug = Normalize(request.Slug); await EnsureUniqueAsync(slug, null, cancellationToken);
        var skill = new Skill(request.Name, slug, request.Category, request.Description);
        db.Skills.Add(skill); await db.SaveChangesAsync(cancellationToken); return ToDto(skill);
    }
    public async Task<SkillDto> UpdateAsync(Guid id, UpdateSkillRequest request, CancellationToken cancellationToken)
    {
        var skill = await FindAsync(id, cancellationToken); RequireDraft(skill.Status, "skill");
        var slug = Normalize(request.Slug); await EnsureUniqueAsync(slug, id, cancellationToken);
        skill.UpdateDetails(request.Name, slug, request.Description); skill.ChangeCategory(request.Category);
        await db.SaveChangesAsync(cancellationToken); return ToDto(skill);
    }
    public async Task PublishAsync(Guid id, CancellationToken cancellationToken)
    { var skill = await FindAsync(id, cancellationToken); skill.Publish(); await db.SaveChangesAsync(cancellationToken); }
    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken)
    { var skill = await FindAsync(id, cancellationToken); skill.Archive(); await db.SaveChangesAsync(cancellationToken); }
    public async Task<SkillDto> GetAdminAsync(Guid id, CancellationToken cancellationToken) => ToDto(await FindAsync(id, cancellationToken));
    public Task<PagedResult<SkillListItemDto>> ListAdminAsync(SkillQuery query, CancellationToken cancellationToken) => ListAsync(query, false, cancellationToken);
    public Task<PagedResult<SkillListItemDto>> ListPublishedAsync(SkillQuery query, CancellationToken cancellationToken) => ListAsync(query, true, cancellationToken);
    public async Task<SkillDto> GetPublishedAsync(string slug, CancellationToken cancellationToken)
    {
        var normalized = Normalize(slug);
        var skill = await db.Skills.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == normalized && x.Status == ContentStatus.Published, cancellationToken)
            ?? throw new NotFoundException("Published skill was not found."); return ToDto(skill);
    }
    private async Task<PagedResult<SkillListItemDto>> ListAsync(SkillQuery request, bool publicOnly, CancellationToken cancellationToken)
    {
        var query = db.Skills.AsNoTracking();
        if (publicOnly) query = query.Where(x => x.Status == ContentStatus.Published); else if (request.Status.HasValue) query = query.Where(x => x.Status == request.Status);
        if (request.Category.HasValue) query = query.Where(x => x.Category == request.Category);
        if (!string.IsNullOrWhiteSpace(request.Search)) { var search = request.Search.Trim().ToLowerInvariant(); query = query.Where(x => x.Name.ToLower().Contains(search) || (x.Description != null && x.Description.ToLower().Contains(search))); }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name).Skip((request.ValidatedPage - 1) * request.ValidatedPageSize).Take(request.ValidatedPageSize)
            .Select(x => new SkillListItemDto(x.Id, x.Name, x.Slug, x.Category, x.Status)).ToListAsync(cancellationToken);
        return new(items, request.ValidatedPage, request.ValidatedPageSize, total);
    }
    private async Task EnsureUniqueAsync(string slug, Guid? excluded, CancellationToken token)
    { if (await db.Skills.AnyAsync(x => x.Slug == slug && (!excluded.HasValue || x.Id != excluded), token)) throw new ConflictException("A skill with this slug already exists."); }
    private async Task<Skill> FindAsync(Guid id, CancellationToken token) => await db.Skills.SingleOrDefaultAsync(x => x.Id == id, token) ?? throw new NotFoundException("Skill was not found.");
    internal static void RequireDraft(ContentStatus status, string resource) { if (status != ContentStatus.Draft) throw new ConflictException($"The {resource} is immutable after publication or archiving."); }
    internal static string Normalize(string slug) => slug.Trim().ToLowerInvariant();
    internal static SkillDto ToDto(Skill x) => new(x.Id, x.Name, x.Slug, x.Description, x.Category, x.Status, x.CreatedAtUtc, x.UpdatedAtUtc);
}
