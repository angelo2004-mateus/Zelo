using Zelo.Domain.Companies;

namespace Zelo.Application.Model.Companies;

public class CompanyDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string LegalName { get; set; }
    public required string TradeName { get; set; }
    public required string FederalTaxNumber { get; set; }
    public CompanyStatus Status { get; set; }
}
