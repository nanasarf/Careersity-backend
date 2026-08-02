using Careersity.Api.Infrastructure;
using Careersity.Application.LearningContent.Requests;
using Careersity.Application.LearningContent.Services;
using Careersity.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Careersity.Api.Controllers;

[ApiController, Authorize(Policy = SecurityPolicies.AdministratorOnly), Route("api/admin/skills"), Tags("Admin Skills")]
public sealed class AdminSkillsController(ISkillService service) : ControllerBase
{
    [HttpPost] public async Task<IActionResult> Create(CreateSkillRequest request, CancellationToken token)
    { var result = await service.CreateAsync(request, token); return CreatedAtAction(nameof(Get), new { id = result.Id }, result); }
    [HttpGet] public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] SkillCategory? category = null,
        [FromQuery] ContentStatus? status = null, CancellationToken token = default) => Ok(await service.ListAdminAsync(new(page, pageSize, search, category, status), token));
    [HttpGet("{id:guid}")] public async Task<IActionResult> Get(Guid id, CancellationToken token) => Ok(await service.GetAdminAsync(id, token));
    [HttpPut("{id:guid}")] public async Task<IActionResult> Update(Guid id, UpdateSkillRequest request, CancellationToken token) => Ok(await service.UpdateAsync(id, request, token));
    [HttpPost("{id:guid}/publish")] public async Task<IActionResult> Publish(Guid id, CancellationToken token) { await service.PublishAsync(id, token); return NoContent(); }
    [HttpPost("{id:guid}/archive")] public async Task<IActionResult> Archive(Guid id, CancellationToken token) { await service.ArchiveAsync(id, token); return NoContent(); }
}
