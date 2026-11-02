using NHibernate;
using NHibernate.Type;
using Zelo.Domain.Users;
using Zelo.Infrastructure.Data.Mappings.Shared;

namespace Zelo.Infrastructure.Data.Mappings.Users;

public class UserAuthTokenMap : EntityClassMapping<UserAuthToken>
{
    public UserAuthTokenMap()
    {
        Table("userauthtokens");
        Property(x => x.UserId, property =>
        {
            property.NotNullable(true);
        });
        Property(x => x.TokenHash, property =>
        {
            property.NotNullable(true);
        });
        Property(x => x.Purpose, property =>
        {
            property.Type<EnumType<UserAuthTokenPurpose>>();
            property.NotNullable(true);
        });
        Property(x => x.ExpirationTime, property =>
        {
            property.Type(NHibernateUtil.UtcDateTime);
            property.NotNullable(true);
        });
        Property(x => x.ConsumptionTime, property =>
        {
            property.Type(NHibernateUtil.UtcDateTime);
        });
        Property(x => x.FailedAttemptCount);
    }
}
