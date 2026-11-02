using Zelo.Domain.Roles;
using Zelo.Infrastructure.Data.Mappings.Shared;

namespace Zelo.Infrastructure.Data.Mappings.Roles;

public class RolePermissionMap : EntityClassMapping<RolePermission>
{
    public RolePermissionMap()
    {
        Table("rolepermissions");
        Filter("tenant", filter => filter.Condition("tenantid = :tenantId"));
        Property(x => x.TenantId, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.RoleId, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.Permission, property =>
        {
            property.NotNullable(true);
        });
    }
}
