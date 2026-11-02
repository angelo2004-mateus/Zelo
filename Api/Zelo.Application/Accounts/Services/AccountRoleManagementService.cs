using Zelo.Application.Contract;
using Zelo.Application.Model.Roles;
using Zelo.Application.Shared.Authorization;
using Zelo.Application.Shared.Data;
using Zelo.Application.Shared.Roles;
using Zelo.Domain.Roles;
using Zelo.Domain.UserTenants;

namespace Zelo.Application.Accounts.Services;

public class AccountRoleManagementService
{
    private readonly IRoleRepository _roles;
    private readonly AccountRoleService _roleWrites;
    private readonly ITenantAuthorizationService _authorization;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserTenantRepository _memberships;

    public AccountRoleManagementService(IRoleRepository roles, AccountRoleService roleWrites, ITenantAuthorizationService authorization, IUnitOfWork unitOfWork, IUserTenantRepository memberships)
    {
        _roles = roles;
        _roleWrites = roleWrites;
        _authorization = authorization;
        _unitOfWork = unitOfWork;
        _memberships = memberships;
    }

    public async Task<AppServiceResult<IReadOnlyList<RoleDto>>> ListAsync(Guid actorId, Guid tenantId, CancellationToken cancellationToken)
    {
        var access = await _authorization.GetAccessAsync(
            actorId, tenantId, cancellationToken);
        if (access is null ||
            !_authorization.HasPermission(access, PermissionCodes.MemberManage))
        {
            return AppServiceResult<IReadOnlyList<RoleDto>>.Failure(
                AppServiceError.Forbidden);
        }

        var roles = await _roles.ListAsync(tenantId, cancellationToken);
        var results = new List<RoleDto>(roles.Count);
        foreach (var role in roles)
        {
            results.Add(new RoleDto
            {
                Id = role.Id,
                TenantId = role.TenantId,
                Name = role.Name,
                Permissions = await _roles.ListPermissionsAsync(
                    tenantId, role.Id, cancellationToken)
            });
        }

        return AppServiceResult<IReadOnlyList<RoleDto>>.Success(results);
    }

    public async Task<AppServiceResult<RoleDto>> CreateAsync(Guid actorId, Guid tenantId, RoleCreateRequestDto request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        var requested = request.Permissions?.Distinct(StringComparer.Ordinal).ToArray();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 ||
            string.Equals(name, "Owner", StringComparison.OrdinalIgnoreCase) ||
            requested is null || requested.Length == 0 ||
            ContainsUnknownPermission(requested))
        {
            return AppServiceResult<RoleDto>.Failure(AppServiceError.Invalid);
        }

        AppServiceResult<RoleDto>? result = null;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _memberships.LockTenantAsync(tenantId, ct);
            var access = await _authorization.GetAccessAsync(actorId, tenantId, ct);
            if (access is null ||
                !_authorization.HasPermission(access, PermissionCodes.MemberManage) ||
                (!access.IsPlatformAdmin &&
                 ExceedsPermissionCeiling(requested, access.Permissions)))
            {
                result = AppServiceResult<RoleDto>.Failure(AppServiceError.Forbidden);
                return;
            }

            var existing = await _roles.ListAsync(tenantId, ct);
            if (existing.Any(role =>
                {
                    return string.Equals(
                        role.Name, name, StringComparison.OrdinalIgnoreCase);
                }))
            {
                result = AppServiceResult<RoleDto>.Failure(AppServiceError.Conflict);
                return;
            }

            var role = await _roleWrites.CreateAsync(
                tenantId, name, requested, ct);

            result = AppServiceResult<RoleDto>.Success(new RoleDto
            {
                Id = role.Id,
                TenantId = role.TenantId,
                Name = role.Name,
                Permissions = requested
            });
        }, cancellationToken);

        return result!;
    }

    public async Task<AppServiceResult<RoleDto>> UpdateAsync(Guid actorId, Guid tenantId, Guid roleId, RoleCreateRequestDto request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        var requested = request.Permissions?.Distinct(StringComparer.Ordinal).ToArray();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 ||
            string.Equals(name, "Owner", StringComparison.OrdinalIgnoreCase) ||
            requested is null || requested.Length == 0 ||
            ContainsUnknownPermission(requested))
        {
            return AppServiceResult<RoleDto>.Failure(AppServiceError.Invalid);
        }

        AppServiceResult<RoleDto>? result = null;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _memberships.LockTenantAsync(tenantId, ct);
            var access = await _authorization.GetAccessAsync(actorId, tenantId, ct);
            if (access is null ||
                !_authorization.HasPermission(access, PermissionCodes.MemberManage) ||
                (!access.IsPlatformAdmin &&
                 ExceedsPermissionCeiling(requested, access.Permissions)))
            {
                result = AppServiceResult<RoleDto>.Failure(AppServiceError.Forbidden);
                return;
            }

            var role = await _roles.FindAsync(tenantId, roleId, ct);
            if (role is null)
            {
                result = AppServiceResult<RoleDto>.Failure(AppServiceError.NotFound);
                return;
            }

            if (string.Equals(role.Name, "Owner", StringComparison.OrdinalIgnoreCase))
            {
                result = AppServiceResult<RoleDto>.Failure(AppServiceError.Forbidden);
                return;
            }

            var existing = await _roles.ListAsync(tenantId, ct);
            if (existing.Any(other =>
                {
                    return other.Id != roleId &&
                           string.Equals(other.Name, name,
                               StringComparison.OrdinalIgnoreCase);
                }))
            {
                result = AppServiceResult<RoleDto>.Failure(AppServiceError.Conflict);
                return;
            }

            await _roleWrites.UpdateAsync(role, name, requested, ct);
            result = AppServiceResult<RoleDto>.Success(new RoleDto
            {
                Id = role.Id,
                TenantId = tenantId,
                Name = role.Name,
                Permissions = requested
            });
        }, cancellationToken);
        return result!;
    }

    private static bool ContainsUnknownPermission(IReadOnlyList<string> permissions)
    {
        foreach (var permission in permissions)
        {
            if (!PermissionCodes.All.Contains(permission))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ExceedsPermissionCeiling(IReadOnlyList<string> requested, IReadOnlyList<string> actorPermissions)
    {
        foreach (var permission in requested)
        {
            if (!actorPermissions.Contains(permission))
            {
                return true;
            }
        }

        return false;
    }
}
