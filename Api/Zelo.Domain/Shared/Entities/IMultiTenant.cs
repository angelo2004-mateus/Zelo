namespace Zelo.Domain.Shared.Entities;

public interface IMultiTenant
{
    Guid TenantId { get; set; }
}
