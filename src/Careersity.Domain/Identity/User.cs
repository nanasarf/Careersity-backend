using Careersity.Domain.Common;
using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.Identity;

/// <summary>A Careersity account and its refresh-token sessions.</summary>
public sealed class User : AuditableEntity
{
    private readonly List<RefreshToken> _refreshTokens = [];
    private User() { }

    public User(string email, string firstName, string lastName, string passwordHash, UserRole role)
    {
        SetEmail(email); SetNames(firstName, lastName);
        PasswordHash = Guard.Required(passwordHash, 1_000, nameof(passwordHash));
        Role = role; IsActive = true;
    }

    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public static string NormalizeEmail(string email) => Guard.Required(email, 320, nameof(email)).ToUpperInvariant();
    public void UpdateName(string firstName, string lastName) { SetNames(firstName, lastName); MarkUpdated(); }
    public void ChangePasswordHash(string passwordHash) { PasswordHash = Guard.Required(passwordHash, 1_000, nameof(passwordHash)); MarkUpdated(); }
    public void RecordSuccessfulLogin(DateTimeOffset atUtc) { LastLoginAtUtc = atUtc.ToUniversalTime(); MarkUpdated(); }
    public void Activate() { if (IsActive) return; IsActive = true; MarkUpdated(); }
    public void Deactivate() { if (!IsActive) return; IsActive = false; MarkUpdated(); }

    public void AddRefreshToken(RefreshToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (token.UserId != Id) throw new DomainException("The refresh token belongs to another user.");
        if (_refreshTokens.Any(x => x.TokenHash == token.TokenHash)) throw new DomainException("Refresh token hash must be unique.");
        _refreshTokens.Add(token); MarkUpdated();
    }

    public void RevokeRefreshToken(Guid tokenId, DateTimeOffset atUtc, string? ip = null, Guid? replacementId = null)
    {
        var token = _refreshTokens.SingleOrDefault(x => x.Id == tokenId) ?? throw new DomainException("Refresh token does not belong to this user.");
        token.Revoke(atUtc, ip, replacementId); MarkUpdated();
    }

    public void RevokeAllActiveRefreshTokens(DateTimeOffset atUtc, string? ip = null)
    {
        foreach (var token in _refreshTokens.Where(x => x.IsActiveAt(atUtc))) token.Revoke(atUtc, ip);
        MarkUpdated();
    }

    private void SetEmail(string email) { Email = Guard.Required(email, 320, nameof(email)); NormalizedEmail = Email.ToUpperInvariant(); }
    private void SetNames(string firstName, string lastName)
    { FirstName = Guard.Required(firstName, 100, nameof(firstName)); LastName = Guard.Required(lastName, 100, nameof(lastName)); }
}
