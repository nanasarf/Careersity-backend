using Careersity.Application.CareerCatalog.Requests;
using Careersity.Application.CareerCatalog.Services;
using Microsoft.AspNetCore.Mvc;

namespace Careersity.Api.Controllers;

/// <summary>Temporarily unsecured career-category administration endpoints.</summary>
[ApiController]
[Route("api/admin/career-categories")]
[Tags("Admin Career Catalog (Temporarily Unsecured)")]
public sealed class AdminCareerCategoriesController(ICareerCategoryService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateCareerCategoryRequest request, CancellationToken cancellationToken)
    { var result = await service.CreateAsync(request, cancellationToken); return CreatedAtAction(nameof(Get), new { id = result.Id }, result); }
    [HttpGet] public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await service.ListAdminAsync(cancellationToken));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken) => Ok(await service.GetAdminAsync(id, cancellationToken));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, UpdateCareerCategoryRequest request, CancellationToken cancellationToken) => Ok(await service.UpdateAsync(id, request, cancellationToken));
    [HttpPost("{id:guid}/publish")] public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken) { await service.PublishAsync(id, cancellationToken); return NoContent(); }
    [HttpPost("{id:guid}/archive")] public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken) { await service.ArchiveAsync(id, cancellationToken); return NoContent(); }
}
