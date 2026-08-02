using Careersity.Application.Abstractions.Persistence;
using Careersity.Application.Common.Exceptions;
using Careersity.Application.Common.Models;
using Careersity.Application.ExternalLearning.Dtos;
using Careersity.Application.ExternalLearning.Requests;
using Careersity.Domain.Enums;
using Careersity.Domain.LearningResources;
using Microsoft.EntityFrameworkCore;

namespace Careersity.Application.ExternalLearning.Services;

public sealed class LearningProviderService(ICareersityDbContext db) : ILearningProviderService
{
    public async Task<LearningProviderDto> CreateAsync(CreateLearningProviderRequest r, CancellationToken t) { await Unique(r.Slug, null, t); var x=new LearningProvider(r.Name,r.Slug,r.Description,r.WebsiteUrl,r.LogoUrl); db.LearningProviders.Add(x); await db.SaveChangesAsync(t); return Map(x); }
    public async Task<LearningProviderDto> UpdateAsync(Guid id, UpdateLearningProviderRequest r, CancellationToken t) { var x=await Find(id,t); await Unique(r.Slug,id,t); x.UpdateDetails(r.Name,r.Slug,r.Description,r.WebsiteUrl,r.LogoUrl); await db.SaveChangesAsync(t); return Map(x); }
    public async Task PublishAsync(Guid id,CancellationToken t){var x=await Find(id,t);x.Publish();await db.SaveChangesAsync(t);} public async Task ArchiveAsync(Guid id,CancellationToken t){var x=await Find(id,t);x.Archive();await db.SaveChangesAsync(t);}
    public async Task<LearningProviderDto> GetAdminAsync(Guid id,CancellationToken t)=>Map(await Find(id,t));
    public async Task<PagedResult<LearningProviderDto>> ListAdminAsync(ExternalLearningQuery r,CancellationToken t){var q=db.LearningProviders.AsNoTracking();if(r.Status.HasValue)q=q.Where(x=>x.Status==r.Status);if(!string.IsNullOrWhiteSpace(r.Search)){var s=r.Search.Trim().ToLower();q=q.Where(x=>x.Name.ToLower().Contains(s));}var count=await q.CountAsync(t);var items=(await q.OrderBy(x=>x.Name).Skip((r.ValidatedPage-1)*r.ValidatedPageSize).Take(r.ValidatedPageSize).ToListAsync(t)).Select(Map).ToList();return new(items,r.ValidatedPage,r.ValidatedPageSize,count);}
    public async Task<IReadOnlyCollection<LearningProviderDto>> ListPublishedAsync(CancellationToken t)=>(await db.LearningProviders.AsNoTracking().Where(x=>x.Status==ContentStatus.Published).OrderBy(x=>x.Name).ToListAsync(t)).Select(Map).ToList();
    public async Task<LearningProviderDto> GetPublishedAsync(string slug,CancellationToken t)=>Map(await db.LearningProviders.AsNoTracking().SingleOrDefaultAsync(x=>x.Slug==slug.Trim().ToLower()&&x.Status==ContentStatus.Published,t)??throw new NotFoundException("Published provider was not found."));
    private async Task<LearningProvider> Find(Guid id,CancellationToken t)=>await db.LearningProviders.SingleOrDefaultAsync(x=>x.Id==id,t)??throw new NotFoundException("Provider was not found.");
    private async Task Unique(string slug,Guid? id,CancellationToken t){var s=slug.Trim().ToLower();if(await db.LearningProviders.AnyAsync(x=>x.Slug==s&&(!id.HasValue||x.Id!=id),t))throw new ConflictException("A provider with this slug already exists.");}
    internal static LearningProviderDto Map(LearningProvider x)=>new(x.Id,x.Name,x.Slug,x.Description,x.WebsiteUrl,x.LogoUrl,x.Status,x.CreatedAtUtc,x.UpdatedAtUtc);
}
