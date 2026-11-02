using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;
using Zelo.Domain.Roles;

namespace Zelo.Infrastructure.Data.Mappings.Roles;

public class RoleMap : ClassMapping<Role>
{
    public RoleMap()
    {
        Table("roles");
        Filter("tenant", filter => filter.Condition("tenantid = :tenantId"));
        Id(x => x.Id, id =>
        {
            id.Generator(Generators.Assigned);
        });

        Property(x => x.TenantId, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.Name, property =>
        {
            property.NotNullable(true);
        });
    }
}
