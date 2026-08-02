using Careersity.Api.Infrastructure;
using Careersity.Application.CurriculumActivities.Requests;
using Careersity.Application.CurriculumActivities.Services;
using Careersity.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Careersity.Api.Controllers;

/// <summary>Administrator-only practical project authoring. Published and Archived projects are immutable.</summary>
[ApiController, Authorize(Policy = SecurityPolicies.AdministratorOnly), Route("api/admin/projects"), Tags("Admin Projects")]
public sealed class AdminProjectsController(IProjectService projects) : ControllerBase
{
    [HttpPost] public async Task<IActionResult> Create(CreateProjectRequest request, CancellationToken token) { var result = await projects.CreateAsync(request, token); return CreatedAtAction(nameof(Get), new { projectId = result.Id }, result); }
    [HttpGet] public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] Guid? courseId = null, [FromQuery] ProjectSubmissionType? submissionType = null, [FromQuery] ContentStatus? status = null, CancellationToken token = default) => Ok(await projects.ListAdminAsync(new(page, pageSize, search, courseId, submissionType, status), token));
    [HttpGet("{projectId:guid}")] public async Task<IActionResult> Get(Guid projectId, CancellationToken token) => Ok(await projects.GetAdminAsync(projectId, token));
    [HttpPut("{projectId:guid}")] public async Task<IActionResult> Update(Guid projectId, UpdateProjectRequest request, CancellationToken token) => Ok(await projects.UpdateAsync(projectId, request, token));
    [HttpPost("{projectId:guid}/publish")] public async Task<IActionResult> Publish(Guid projectId, CancellationToken token) { await projects.PublishAsync(projectId, token); return NoContent(); }
    [HttpPost("{projectId:guid}/archive")] public async Task<IActionResult> Archive(Guid projectId, CancellationToken token) { await projects.ArchiveAsync(projectId, token); return NoContent(); }
}
