using Careersity.Domain.Enums;
using Careersity.Domain.Exceptions;

namespace Careersity.Domain.Common;

/// <summary>Provides the shared draft, published, and archived lifecycle.</summary>
public abstract class PublishableEntity : AuditableEntity
{
    protected PublishableEntity() => Status = ContentStatus.Draft;

    public ContentStatus Status { get; protected set; }

    /// <summary>Publishes draft content. Publishing already-published content is idempotent.</summary>
    public virtual void Publish()
    {
        if (Status == ContentStatus.Archived)
        {
            throw new DomainException("Archived content cannot be published directly.");
        }

        if (Status == ContentStatus.Published)
        {
            return;
        }

        Status = ContentStatus.Published;
        MarkUpdated();
    }

    /// <summary>Archives draft or published content. Repeated archiving is idempotent.</summary>
    public void Archive()
    {
        if (Status == ContentStatus.Archived)
        {
            return;
        }

        Status = ContentStatus.Archived;
        MarkUpdated();
    }
}
