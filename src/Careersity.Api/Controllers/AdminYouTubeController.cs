using Careersity.Api.Infrastructure;
using Careersity.Application.Abstractions.Authentication;
using Careersity.Application.YouTube.Requests;
using Careersity.Application.YouTube.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Careersity.Api.Controllers;

[ApiController]
[Authorize(Policy = SecurityPolicies.AdministratorOnly)]
[EnableRateLimiting(SecurityPolicies.YouTubeSearchRateLimit)]
[Route("api/admin/youtube")]
[Tags("Admin External Learning")]
public sealed class AdminYouTubeController(
    IYouTubeSearchService service,
    ICurrentUser currentUser,
    ILogger<AdminYouTubeController> logger) : ControllerBase
{
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] YouTubeSearchRequest request, CancellationToken cancellationToken)
    {
        var result = await service.SearchAsync(request, cancellationToken);
        logger.LogInformation("Administrator {UserId} searched YouTube and received {ResultCount} candidates",
            currentUser.UserId, result.Items.Count);
        return Ok(result);
    }
}
