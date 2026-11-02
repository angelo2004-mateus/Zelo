using NHibernate.Type;
using Zelo.Domain.Accounts;
using Zelo.Infrastructure.Data.Mappings.Shared;

namespace Zelo.Infrastructure.Data.Mappings.Accounts;

public class AccountMap : AuditedAggregateClassMapping<Account>
{
    public AccountMap()
    {
        Table("accounts");
        Property(x => x.Name, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.Status, property =>
        {
            property.Type<EnumType<AccountStatus>>();
            property.NotNullable(true);
        });
    }
}
