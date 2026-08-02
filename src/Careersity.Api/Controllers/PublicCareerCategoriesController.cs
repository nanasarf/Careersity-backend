using Careersity.Application.CareerCatalog.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Careersity.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/career-categories")]
[Tags("Public Career Catalog")]
public sealed class PublicCareerCategoriesController(ICareerCategoryService service) : ControllerBase
{
    /// <summary>Lists published career categories.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok(await service.ListPublishedAsync(cancellationToken));
}
