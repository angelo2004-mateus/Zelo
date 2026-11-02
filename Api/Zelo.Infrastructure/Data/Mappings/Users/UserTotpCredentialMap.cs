using NHibernate;
using Zelo.Domain.Users;
using Zelo.Infrastructure.Data.Mappings.Shared;

namespace Zelo.Infrastructure.Data.Mappings.Users;

public class UserTotpCredentialMap : EntityClassMapping<UserTotpCredential>
{
    public UserTotpCredentialMap()
    {
        Table("usertotpcredentials");
        Property(x => x.UserId, property =>
        {
            property.NotNullable(true);
        });
        Property(x => x.ProtectedSecret, property =>
        {
            property.NotNullable(true);
        });
        Property(x => x.EnabledTime, property =>
        {
            property.Type(NHibernateUtil.UtcDateTime);
        });
        Property(x => x.LastUsedStep);
        Property(x => x.FailedAttemptCount);
        Property(x => x.LockoutEndTime, property =>
        {
            property.Type(NHibernateUtil.UtcDateTime);
        });
    }
}
