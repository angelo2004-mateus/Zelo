using Zelo.Domain.Shared.Entities.Audited;

namespace Zelo.Domain.Shared.Entities;

public abstract class AuditedAggregateRoot : AggregateRoot,
    IHasCreationTime, IHasLastModifiedTime, IHasCreatorId, IHasLastModifierId
{
    public virtual Guid CreatorId { get; set; }
    public virtual Guid? LastModifierId { get; set; }

    public AuditedAggregateRoot() { }
}
