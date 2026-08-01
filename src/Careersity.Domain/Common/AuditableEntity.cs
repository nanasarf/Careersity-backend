namespace Careersity.Domain.Common;

/// <summary>Base type for entities that track UTC creation and modification times.</summary>
public abstract class AuditableEntity : Entity
{
    protected AuditableEntity() => CreatedAtUtc = DateTimeOffset.UtcNow;

    public DateTimeOffset CreatedAtUtc { get; protected set; }
    public DateTimeOffset? UpdatedAtUtc { get; protected set; }

    protected void MarkUpdated() => UpdatedAtUtc = DateTimeOffset.UtcNow;
}
