using Zelo.Domain.Users;
using Zelo.Infrastructure.Data.Mappings.Shared;

namespace Zelo.Infrastructure.Data.Mappings.Users;

public class UserPasswordCredentialMap : EntityClassMapping<UserPasswordCredential>
{
    public UserPasswordCredentialMap()
    {
        Table("userpasswordcredentials");
        Property(x => x.UserId, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.PasswordHash, property =>
        {
            property.NotNullable(true);
        });
        Property(x => x.FailedLoginCount);
        Property(x => x.LockoutEndTime, property =>
        {
            property.Type(NHibernate.NHibernateUtil.UtcDateTime);
        });
    }
}
