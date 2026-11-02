using Zelo.Application.Shared.Roles;
using Zelo.Domain.Roles;
using Zelo.Domain.UserTenants;

namespace Zelo.Application.Shared.Authorization;

public class TenantAuthorizationService : ITenantAuthorizationService
{
    private readonly IUserTenantRepository _memberships;
    private readonly IRoleRepository _roles;

    public TenantAuthorizationService(IUserTenantRepository memberships, IRoleRepository roles)
    {
        _memberships = memberships;
        _roles = roles;
    }

    public Task<TenantAccess?> GetAccessAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        return _memberships.GetAccessAsync(userId, tenantId, cancellationToken);
    }

    public bool HasPermission(TenantAccess access, string permission)
    {
        if (!PermissionCodes.All.Contains(permission, StringComparer.Ordinal))
        {
            return false;
        }

        return access.IsPlatformAdmin || access.Permissions.Contains(permission);
    }

    public async Task<bool> CanGrantRoleAsync(TenantAccess access, Role role, CancellationToken cancellationToken)
    {
        if (role.TenantId != access.TenantId ||
            !HasPermission(access, PermissionCodes.MemberManage))
        {
            return false;
        }

        if (string.Equals(role.Name, "Owner", StringComparison.OrdinalIgnoreCase))
        {
            return access.IsOwner || access.IsPlatformAdmin;
        }

        if (access.IsPlatformAdmin)
        {
            return true;
        }

        var rolePermissions = await _roles.ListPermissionsAsync(
            access.TenantId, role.Id, cancellationToken);
        return rolePermissions.All(access.Permissions.Contains);
    }
}
