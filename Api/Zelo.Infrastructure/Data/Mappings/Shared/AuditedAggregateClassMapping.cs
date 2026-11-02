using Zelo.Domain.Shared.Entities;

namespace Zelo.Infrastructure.Data.Mappings.Shared;

public abstract class AuditedAggregateClassMapping<TEntity> : EntityClassMapping<TEntity>
    where TEntity : AuditedAggregateRoot
{
    protected AuditedAggregateClassMapping()
    {
        Property(x => x.CreatorId, property =>
        {
            property.NotNullable(true);
        });
        Property(x => x.LastModifierId);
    }
}
