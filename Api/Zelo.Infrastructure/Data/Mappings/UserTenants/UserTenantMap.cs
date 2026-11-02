using NHibernate;
using NHibernate.Type;
using Zelo.Domain.UserTenants;
using Zelo.Infrastructure.Data.Mappings.Shared;

namespace Zelo.Infrastructure.Data.Mappings.UserTenants;

public class UserTenantMap : AuditedAggregateClassMapping<UserTenant>
{
    public UserTenantMap()
    {
        Table("usertenants");
        Filter("tenant", filter => filter.Condition("tenantid = :tenantId"));
        Property(x => x.UserId, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.TenantId, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.RoleId, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.Status, property =>
        {
            property.Type<EnumType<UserTenantStatus>>();
            property.NotNullable(true);
        });

        Property(x => x.JoinTime, property =>
        {
            property.Type(NHibernateUtil.UtcDateTime);
            property.NotNullable(true);
        });
    }
}
