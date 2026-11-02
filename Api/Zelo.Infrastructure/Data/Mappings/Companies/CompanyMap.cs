using NHibernate.Type;
using Zelo.Domain.Companies;
using Zelo.Infrastructure.Data.Mappings.Shared;

namespace Zelo.Infrastructure.Data.Mappings.Companies;

public class CompanyMap : AuditedAggregateClassMapping<Company>
{
    public CompanyMap()
    {
        Table("companies");
        Filter("tenant", filter => filter.Condition("tenantid = :tenantId"));
        Property(x => x.TenantId, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.LegalName, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.TradeName, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.FederalTaxNumber, property =>
        {
            property.NotNullable(true);
        });

        Property(x => x.Status, property =>
        {
            property.Type<EnumType<CompanyStatus>>();
            property.NotNullable(true);
        });
    }
}
