using Careersity.Application.CareerCatalog.Requests;
using Careersity.Application.CareerCatalog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Careersity.Api.Infrastructure;

namespace Careersity.Api.Controllers;

/// <summary>Administrator-only career-skill assignment endpoints.</summary>
[ApiController]
[Authorize(Policy = SecurityPolicies.AdministratorOnly)]
[Route("api/admin/careers/{careerId:guid}/skills")]
[Tags("Administrator Career Catalog")]
public sealed class AdminCareerSkillsController(ICareerSkillService service) : ControllerBase
{
    [HttpGet] public async Task<IActionResult> List(Guid careerId, CancellationToken cancellationToken) => Ok(await service.ListAsync(careerId, false, cancellationToken));
    [HttpPost] public async Task<IActionResult> Assign(Guid careerId, AssignCareerSkillRequest request, CancellationToken cancellationToken) { var result = await service.AssignAsync(careerId, request, cancellationToken); return CreatedAtAction(nameof(List), new { careerId }, result); }
    [HttpPut("{careerSkillId:guid}")] public async Task<IActionResult> Update(Guid careerId, Guid careerSkillId, UpdateCareerSkillRequest request, CancellationToken cancellationToken) => Ok(await service.UpdateAsync(careerId, careerSkillId, request, cancellationToken));
    [HttpDelete("{careerSkillId:guid}")] public async Task<IActionResult> Remove(Guid careerId, Guid careerSkillId, CancellationToken cancellationToken) { await service.RemoveAsync(careerId, careerSkillId, cancellationToken); return NoContent(); }
}
