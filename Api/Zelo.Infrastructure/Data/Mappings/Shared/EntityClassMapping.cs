using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Zelo.Domain.Shared.Entities;

namespace Zelo.Infrastructure.Data.Mappings.Shared;

public abstract class EntityClassMapping<TEntity> : ClassMapping<TEntity>
    where TEntity : Entity
{
    protected EntityClassMapping()
    {
        Id(x => x.Id, id =>
        {
            id.Generator(Generators.Assigned);
        });

        Property(x => x.CreationTime, property =>
        {
            property.Type(NHibernateUtil.UtcDateTime);
            property.NotNullable(true);
        });

        Property(x => x.LastModifiedTime, property =>
        {
            property.Type(NHibernateUtil.UtcDateTime);
        });
    }
}
