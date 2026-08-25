using Careersity.Application.CareerCatalog.Dtos;
using Careersity.Application.CareerCatalog.Requests;
using Careersity.Application.CareerCatalog.Services;
using Careersity.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Careersity.Api.Infrastructure;

namespace Careersity.Api.Controllers;

/// <summary>Administrator-only career management endpoints.</summary>
[ApiController]
[Authorize(Policy = SecurityPolicies.AdministratorOnly)]
[Route("api/admin/careers")]
[Tags("Administrator Career Catalog")]
public sealed class AdminCareersController(ICareerService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateCareerRequest request, CancellationToken cancellationToken)
    { var result = await service.CreateAsync(request, cancellationToken); return CreatedAtAction(nameof(Get), new { id = result.Id }, result); }
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null,
        [FromQuery] Guid? categoryId = null, [FromQuery] ContentStatus? status = null, CancellationToken cancellationToken = default) =>
        Ok(await service.ListAdminAsync(new CareerQuery(page, pageSize, search, categoryId, status), cancellationToken));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) => Ok(await service.GetAdminAsync(id, cancellationToken));
    [HttpGet("{id:guid}/readiness")] public async Task<IActionResult> Readiness(Guid id, CancellationToken cancellationToken) => Ok(await service.GetReadinessAsync(id, cancellationToken));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, UpdateCareerRequest request, CancellationToken cancellationToken) => Ok(await service.UpdateAsync(id, request, cancellationToken));
    [HttpPut("{id:guid}/category")] public async Task<IActionResult> ChangeCategory(Guid id, ChangeCareerCategoryRequest request, CancellationToken cancellationToken) { await service.ChangeCategoryAsync(id, request, cancellationToken); return NoContent(); }
    [HttpPost("{id:guid}/publish")] public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken) { await service.PublishAsync(id, cancellationToken); return NoContent(); }
    [HttpPost("{id:guid}/archive")] public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken) { await service.ArchiveAsync(id, cancellationToken); return NoContent(); }
}
