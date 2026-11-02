using Zelo.Domain.Roles;
using Zelo.Domain.UserTenants;

namespace Zelo.Application.Shared.Authorization;

public interface ITenantAuthorizationService
{
    Task<TenantAccess?> GetAccessAsync(
        Guid userId, Guid tenantId, CancellationToken cancellationToken);
    bool HasPermission(TenantAccess access, string permission);
    Task<bool> CanGrantRoleAsync(
        TenantAccess access, Role role, CancellationToken cancellationToken);
}
