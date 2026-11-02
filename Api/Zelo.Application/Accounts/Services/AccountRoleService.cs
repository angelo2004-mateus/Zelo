using Zelo.Application.Shared.Roles;
using Zelo.Domain.Accounts;
using Zelo.Domain.Roles;

namespace Zelo.Application.Accounts.Services;

public class AccountRoleService
{
    private readonly IAccountRepository _accounts;

    public AccountRoleService(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<Role> CreateAsync(Guid tenantId, string name, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        if (permissions.Count == 0 || permissions.Any(permission =>
            {
                return !PermissionCodes.All.Contains(permission);
            }))
        {
            throw new ArgumentException("The role requires known permissions.",
                nameof(permissions));
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name
        };
        await _accounts.AddRoleAsync(role, cancellationToken);
        foreach (var permission in permissions.Distinct(StringComparer.Ordinal))
        {
            await _accounts.AddRolePermissionAsync(new RolePermission
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RoleId = role.Id,
                Permission = permission,
            }, cancellationToken);
        }

        return role;
    }

    public async Task UpdateAsync(Role role, string name, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        if (permissions.Count == 0 || permissions.Any(permission =>
            {
                return !PermissionCodes.All.Contains(permission);
            }))
        {
            throw new ArgumentException("The role requires known permissions.",
                nameof(permissions));
        }

        role.Name = name;
        await _accounts.UpdateRoleAsync(role, cancellationToken);
        var existing = await _accounts.ListRolePermissionEntitiesAsync(
            role.TenantId, role.Id, cancellationToken);
        var requested = new HashSet<string>(permissions, StringComparer.Ordinal);
        var present = new HashSet<string>(StringComparer.Ordinal);
        foreach (var permission in existing)
        {
            if (requested.Contains(permission.Permission))
            {
                present.Add(permission.Permission);
            }
            else
            {
                await _accounts.DeleteRolePermissionAsync(
                    permission, cancellationToken);
            }
        }

        foreach (var code in requested)
        {
            if (!present.Contains(code))
            {
                await _accounts.AddRolePermissionAsync(new RolePermission
                {
                    Id = Guid.NewGuid(),
                    TenantId = role.TenantId,
                    RoleId = role.Id,
                    Permission = code,
                }, cancellationToken);
            }
        }
    }
}
