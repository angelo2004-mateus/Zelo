using Zelo.Domain.Shared.Entities;

namespace Zelo.Domain.Companies;

public class Company : AuditedAggregateRoot, IMultiTenant
{
    public virtual Guid TenantId { get; set; }
    public virtual string LegalName { get; set; }
    public virtual string TradeName { get; set; }
    public virtual string FederalTaxNumber { get; set; }
    public virtual CompanyStatus Status { get; set; }

    public Company() { }
}
