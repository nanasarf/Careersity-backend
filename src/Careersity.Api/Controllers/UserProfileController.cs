using Careersity.Application.Identity.Requests;
using Careersity.Application.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Careersity.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me")]
[Tags("Authenticated User Profile")]
public sealed class UserProfileController(IUserProfileService profiles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken) => Ok(await profiles.GetAsync(cancellationToken));

    [HttpPut]
    public async Task<IActionResult> Update(UpdateMyProfileRequest request, CancellationToken cancellationToken) =>
        Ok(await profiles.UpdateAsync(request, cancellationToken));
}
