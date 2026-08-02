using Careersity.Application.LearningContent.Requests;
using Careersity.Application.LearningContent.Services;
using Careersity.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Careersity.Api.Controllers;

[ApiController, AllowAnonymous, Route("api/skills"), Tags("Public Learning Content")]
public sealed class PublicSkillsController(ISkillService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] SkillCategory? category = null, CancellationToken cancellationToken = default) =>
        Ok(await service.ListPublishedAsync(new(page, pageSize, search, category), cancellationToken));
    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug, CancellationToken cancellationToken) => Ok(await service.GetPublishedAsync(slug, cancellationToken));
}
