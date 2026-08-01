namespace Careersity.Domain.Common;

/// <summary>Base type for domain entities identified by a GUID.</summary>
public abstract class Entity
{
    protected Entity() => Id = Guid.NewGuid();

    public Guid Id { get; protected set; }
}
