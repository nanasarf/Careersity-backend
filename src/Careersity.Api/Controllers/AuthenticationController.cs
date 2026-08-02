using Careersity.Api.Infrastructure;
using Careersity.Application.Identity.Requests;
using Careersity.Application.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Careersity.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Authentication")]
public sealed class AuthenticationController(IAuthenticationService authentication) : ControllerBase
{
    [AllowAnonymous, HttpPost("register"), EnableRateLimiting(SecurityPolicies.RegistrationRateLimit)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await authentication.RegisterAsync(request, ClientIp, cancellationToken));

    [AllowAnonymous, HttpPost("login"), EnableRateLimiting(SecurityPolicies.LoginRateLimit)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await authentication.LoginAsync(request, ClientIp, cancellationToken));

    [AllowAnonymous, HttpPost("refresh"), EnableRateLimiting(SecurityPolicies.RefreshRateLimit)]
    public async Task<IActionResult> Refresh(RefreshAccessTokenRequest request, CancellationToken cancellationToken) =>
        Ok(await authentication.RefreshAsync(request, ClientIp, cancellationToken));

    [Authorize, HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    { await authentication.LogoutAsync(request, ClientIp, cancellationToken); return NoContent(); }

    [Authorize, HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAll(CancellationToken cancellationToken)
    { await authentication.RevokeAllAsync(ClientIp, cancellationToken); return NoContent(); }

    [Authorize, HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangeMyPasswordRequest request, CancellationToken cancellationToken) =>
        Ok(await authentication.ChangePasswordAsync(request, ClientIp, cancellationToken));

    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
}
