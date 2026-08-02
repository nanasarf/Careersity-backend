using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Careersity.Application.Abstractions.Authentication;
using Careersity.Domain.Enums;

namespace Careersity.Api.Infrastructure;

internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;
    public string? Email => Principal?.FindFirstValue(JwtRegisteredClaimNames.Email);
    public UserRole? Role => Enum.TryParse<UserRole>(Principal?.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;
}
