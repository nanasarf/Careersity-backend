using Careersity.Application.CareerCatalog.Requests;
using Careersity.Application.CareerCatalog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Careersity.Api.Infrastructure;

namespace Careersity.Api.Controllers;

/// <summary>Administrator-only career-pathway management endpoints.</summary>
[ApiController]
[Authorize(Policy = SecurityPolicies.AdministratorOnly)]
[Route("api/admin/careers/{careerId:guid}/pathways")]
[Tags("Administrator Career Catalog")]
public sealed class AdminCareerPathwaysController(ICareerPathwayService service) : ControllerBase
{
    [HttpPost] public async Task<IActionResult> Create(Guid careerId, CreateCareerPathwayRequest request, CancellationToken cancellationToken) { var result = await service.CreateAsync(careerId, request, cancellationToken); return CreatedAtAction(nameof(Get), new { careerId, pathwayId = result.Id }, result); }
    [HttpGet] public async Task<IActionResult> List(Guid careerId, CancellationToken cancellationToken) => Ok(await service.ListAdminAsync(careerId, cancellationToken));
    [HttpGet("{pathwayId:guid}")] public async Task<IActionResult> Get(Guid careerId, Guid pathwayId, CancellationToken cancellationToken) => Ok(await service.GetAdminAsync(careerId, pathwayId, cancellationToken));
    [HttpPut("{pathwayId:guid}")] public async Task<IActionResult> Update(Guid careerId, Guid pathwayId, UpdateCareerPathwayRequest request, CancellationToken cancellationToken) => Ok(await service.UpdateAsync(careerId, pathwayId, request, cancellationToken));
    [HttpPost("{pathwayId:guid}/publish")] public async Task<IActionResult> Publish(Guid careerId, Guid pathwayId, CancellationToken cancellationToken) { await service.PublishAsync(careerId, pathwayId, cancellationToken); return NoContent(); }
    [HttpPost("{pathwayId:guid}/archive")] public async Task<IActionResult> Archive(Guid careerId, Guid pathwayId, CancellationToken cancellationToken) { await service.ArchiveAsync(careerId, pathwayId, cancellationToken); return NoContent(); }
    [HttpPost("{pathwayId:guid}/levels")] public async Task<IActionResult> AddLevel(Guid careerId, Guid pathwayId, AddPathwayLevelRequest request, CancellationToken cancellationToken) { var result = await service.AddLevelAsync(careerId, pathwayId, request, cancellationToken); return Created(string.Empty, result); }
    [HttpPut("{pathwayId:guid}/levels/reorder")] public async Task<IActionResult> ReorderLevels(Guid careerId, Guid pathwayId, ReorderPathwayLevelsRequest request, CancellationToken cancellationToken) { await service.ReorderLevelsAsync(careerId, pathwayId, request, cancellationToken); return NoContent(); }
    [HttpPut("{pathwayId:guid}/levels/{levelId:guid}")] public async Task<IActionResult> UpdateLevel(Guid careerId, Guid pathwayId, Guid levelId, UpdatePathwayLevelRequest request, CancellationToken cancellationToken) => Ok(await service.UpdateLevelAsync(careerId, pathwayId, levelId, request, cancellationToken));
    [HttpDelete("{pathwayId:guid}/levels/{levelId:guid}")] public async Task<IActionResult> RemoveLevel(Guid careerId, Guid pathwayId, Guid levelId, CancellationToken cancellationToken) { await service.RemoveLevelAsync(careerId, pathwayId, levelId, cancellationToken); return NoContent(); }
    [HttpPost("{pathwayId:guid}/levels/{levelId:guid}/courses")] public async Task<IActionResult> AddCourse(Guid careerId, Guid pathwayId, Guid levelId, AddPathwayLevelCourseRequest request, CancellationToken cancellationToken) { var result = await service.AddCourseAsync(careerId, pathwayId, levelId, request, cancellationToken); return Created(string.Empty, result); }
    [HttpPut("{pathwayId:guid}/levels/{levelId:guid}/courses/reorder")] public async Task<IActionResult> ReorderCourses(Guid careerId, Guid pathwayId, Guid levelId, ReorderPathwayCoursesRequest request, CancellationToken cancellationToken) { await service.ReorderCoursesAsync(careerId, pathwayId, levelId, request, cancellationToken); return NoContent(); }
    [HttpPut("{pathwayId:guid}/levels/{levelId:guid}/courses/{assignmentId:guid}")] public async Task<IActionResult> UpdateCourse(Guid careerId, Guid pathwayId, Guid levelId, Guid assignmentId, UpdatePathwayLevelCourseRequest request, CancellationToken cancellationToken) => Ok(await service.UpdateCourseAsync(careerId, pathwayId, levelId, assignmentId, request, cancellationToken));
    [HttpDelete("{pathwayId:guid}/levels/{levelId:guid}/courses/{assignmentId:guid}")] public async Task<IActionResult> RemoveCourse(Guid careerId, Guid pathwayId, Guid levelId, Guid assignmentId, CancellationToken cancellationToken) { await service.RemoveCourseAsync(careerId, pathwayId, levelId, assignmentId, cancellationToken); return NoContent(); }
}
