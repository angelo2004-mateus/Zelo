using Zelo.Domain.Shared.Repositories;

namespace Zelo.Domain.UserTenants;

public interface IUserTenantRepository : IRepository<UserTenant, Guid>
{
    Task<TenantAccess?> GetAccessAsync(
        Guid userId, Guid tenantId, CancellationToken cancellationToken);
    Task<UserTenant?> FindAsync(
        Guid userId, Guid tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TenantMember>> ListDetailsAsync(
        Guid tenantId, CancellationToken cancellationToken);
    Task<int> CountActiveOwnersAsync(Guid tenantId, CancellationToken cancellationToken);
    Task LockTenantAsync(Guid tenantId, CancellationToken cancellationToken);
}
