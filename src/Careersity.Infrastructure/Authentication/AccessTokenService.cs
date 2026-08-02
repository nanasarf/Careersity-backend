using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Careersity.Application.Abstractions.Authentication;
using Careersity.Domain.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Careersity.Infrastructure.Authentication;

internal sealed class AccessTokenService(IOptions<JwtOptions> options, IDateTimeProvider clock) : IAccessTokenService
{
    public AccessTokenResult Create(User user)
    {
        var settings = options.Value;
        var now = clock.UtcNow;
        var expires = now.AddMinutes(settings.AccessTokenLifetimeMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims,
            notBefore: now.UtcDateTime, expires: expires.UtcDateTime, signingCredentials: credentials);
        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
