namespace Zelo.Domain.Shared.Entities;

public interface ICompanyScoped : IMultiTenant
{
    Guid CompanyId { get; set; }
}
