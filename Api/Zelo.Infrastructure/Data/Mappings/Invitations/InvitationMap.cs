using NHibernate;
using NHibernate.Type;
using Zelo.Domain.Invitations;
using Zelo.Infrastructure.Data.Mappings.Shared;

namespace Zelo.Infrastructure.Data.Mappings.Invitations;

public class InvitationMap : EntityClassMapping<Invitation>
{
    public InvitationMap()
    {
        Table("invitations");
        Filter("tenant", filter => filter.Condition("tenantid = :tenantId"));
        Property(x => x.TenantId, property =>
        {
            property.NotNullable(true);
        });
        Property(x => x.Email, property =>
        {
            property.NotNullable(true);
        });
        Property(x => x.RoleId, property =>
        {
            property.NotNullable(true);
        });
        Property(x => x.InvitedByUserId, property =>
        {
            property.NotNullable(true);
        });
        Property(x => x.Status, property =>
        {
            property.Type<EnumType<InvitationStatus>>();
            property.NotNullable(true);
        });
        Property(x => x.TokenHash, property =>
        {
            property.NotNullable(true);
        });
        Property(x => x.ExpirationTime, property =>
        {
            property.Type(NHibernateUtil.UtcDateTime);
            property.NotNullable(true);
        });
        Property(x => x.AcceptedTime, property =>
        {
            property.Type(NHibernateUtil.UtcDateTime);
        });
    }
}
