using Careersity.Domain.Common;

namespace Careersity.Domain.Identity;

/// <summary>A hashed, rotatable credential used to obtain new access tokens.</summary>
public sealed class RefreshToken : Entity
{
    private RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc, string? createdByIp = null)
    {
        UserId = Guard.NotEmpty(userId, nameof(userId));
        TokenHash = Guard.Required(tokenHash, 128, nameof(tokenHash));
        if (expiresAtUtc <= createdAtUtc) throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "Expiration must follow creation.");
        CreatedAtUtc = createdAtUtc.ToUniversalTime(); ExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        CreatedByIp = Guard.Optional(createdByIp, 64, nameof(createdByIp));
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? RevokedByIp { get; private set; }

    public bool IsActiveAt(DateTimeOffset nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;

    public void Revoke(DateTimeOffset revokedAtUtc, string? revokedByIp = null, Guid? replacedByTokenId = null)
    {
        if (RevokedAtUtc is not null) return;
        if (revokedAtUtc < CreatedAtUtc) throw new ArgumentOutOfRangeException(nameof(revokedAtUtc));
        RevokedAtUtc = revokedAtUtc.ToUniversalTime();
        RevokedByIp = Guard.Optional(revokedByIp, 64, nameof(revokedByIp));
        ReplacedByTokenId = replacedByTokenId;
    }
}
