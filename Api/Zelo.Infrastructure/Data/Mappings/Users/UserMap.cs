using NHibernate;
using NHibernate.Type;
using Zelo.Domain.Users;
using Zelo.Infrastructure.Data.Mappings.Shared;

namespace Zelo.Infrastructure.Data.Mappings.Users;

public class UserMap : EntityClassMapping<User>
{
    public UserMap()
    {
        Table("users");
        Property(x => x.Email, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.Name, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.UserRole, property =>
        {
            property.Type<EnumType<UserRole>>();
            property.NotNullable(true);
        });

        Property(x => x.Status, property =>
        {
            property.Type<EnumType<UserStatus>>();
            property.NotNullable(true);
        });
        Property(x => x.EmailConfirmationTime, property =>
        {
            property.Type(NHibernateUtil.UtcDateTime);
        });
    }
}
