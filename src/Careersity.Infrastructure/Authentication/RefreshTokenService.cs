using System.Security.Cryptography;
using Careersity.Application.Abstractions.Authentication;
using Microsoft.Extensions.Options;

namespace Careersity.Infrastructure.Authentication;

internal sealed class RefreshTokenService(IOptions<JwtOptions> options) : IRefreshTokenService
{
    public TimeSpan Lifetime { get; } = TimeSpan.FromDays(options.Value.RefreshTokenLifetimeDays);

    public RefreshTokenResult Generate()
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new RefreshTokenResult(rawToken, Hash(rawToken));
    }

    public string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
    }
}
