using NHibernate;
using Zelo.Domain.Users;
using Zelo.Infrastructure.Data.Mappings.Shared;

namespace Zelo.Infrastructure.Data.Mappings.Users;

public class UserAuthSessionMap : EntityClassMapping<UserAuthSession>
{
    public UserAuthSessionMap()
    {
        Table("userauthsessions");
        Property(x => x.UserId, property =>
        {
            property.NotNullable(true);
        });
        Property(x => x.RefreshTokenHash, property =>
        {
            property.NotNullable(true);
        });
        Property(x => x.ExpirationTime, property =>
        {
            property.Type(NHibernateUtil.UtcDateTime);
            property.NotNullable(true);
        });
        Property(x => x.RevocationTime, property =>
        {
            property.Type(NHibernateUtil.UtcDateTime);
        });
    }
}
