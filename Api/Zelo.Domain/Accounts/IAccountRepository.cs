using Zelo.Domain.Shared.Repositories;
using Zelo.Domain.Roles;

namespace Zelo.Domain.Accounts;

public interface IAccountRepository : IRepository<Account, Guid>
{
    Task<IReadOnlyList<AccountAccess>> ListAccessibleAsync(
        Guid userId, CancellationToken cancellationToken = default);
    Task<AccountAccess?> GetAccessibleAsync(
        Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    Task AddRoleAsync(Role role, CancellationToken cancellationToken);
    Task AddRolePermissionAsync(
        RolePermission permission, CancellationToken cancellationToken);
    Task UpdateRoleAsync(Role role, CancellationToken cancellationToken);
    Task<IReadOnlyList<RolePermission>> ListRolePermissionEntitiesAsync(
        Guid tenantId, Guid roleId, CancellationToken cancellationToken);
    Task DeleteRolePermissionAsync(
        RolePermission permission, CancellationToken cancellationToken);
}
