using Careersity.Application.CareerCatalog.Dtos;
using Careersity.Application.CareerCatalog.Services;
using Microsoft.AspNetCore.Mvc;

namespace Careersity.Api.Controllers;

[ApiController]
[Route("api/careers")]
[Tags("Public Career Catalog")]
public sealed class PublicCareersController(ICareerService careers, ICareerPathwayService pathways) : ControllerBase
{
    /// <summary>Searches published careers.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] Guid? categoryId = null, CancellationToken cancellationToken = default) =>
        Ok(await careers.ListPublishedAsync(new CareerQuery(page, pageSize, search, categoryId), cancellationToken));

    /// <summary>Gets a published career by slug.</summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> Get(string slug, CancellationToken cancellationToken) =>
        Ok(await careers.GetPublishedBySlugAsync(slug, cancellationToken));

    /// <summary>Gets the published primary pathway for a published career.</summary>
    [HttpGet("{careerId:guid}/pathway")]
    public async Task<IActionResult> GetPathway(Guid careerId, CancellationToken cancellationToken) =>
        Ok(await pathways.GetPublicPrimaryAsync(careerId, cancellationToken));
}
