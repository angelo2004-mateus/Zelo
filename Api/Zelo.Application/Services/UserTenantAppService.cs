using Zelo.Application.Contract;
using Zelo.Application.Model.UserTenants;
using Zelo.Application.Shared.Authorization;
using Zelo.Application.Shared.Context;
using Zelo.Application.Shared.Data;
using Zelo.Application.Shared.Roles;
using Zelo.Domain.Roles;
using Zelo.Domain.UserTenants;

namespace Zelo.Application.Services;

public class UserTenantAppService : IUserTenantAppService
{
    private readonly IUserTenantRepository _memberships;
    private readonly IRoleRepository _roles;
    private readonly ITenantAuthorizationService _authorization;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;

    public UserTenantAppService(IUserTenantRepository memberships, IRoleRepository roles, ITenantAuthorizationService authorization, IUnitOfWork unitOfWork, ICurrentUser currentUser, ICurrentTenant currentTenant)
    {
        _memberships = memberships;
        _roles = roles;
        _authorization = authorization;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
    }

    public async Task<AppServiceResult<IReadOnlyList<UserTenantMemberDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not Guid actorId || _currentTenant.Id is not Guid tenantId)
        {
            return AppServiceResult<IReadOnlyList<UserTenantMemberDto>>.Failure(AppServiceError.Forbidden);
        }

        var access = await _authorization.GetAccessAsync(
            actorId, tenantId, cancellationToken);
        if (access is null ||
            !_authorization.HasPermission(access, PermissionCodes.MemberManage))
        {
            return AppServiceResult<IReadOnlyList<UserTenantMemberDto>>.Failure(
                AppServiceError.Forbidden);
        }

        var members = await _memberships.ListDetailsAsync(tenantId, cancellationToken);
        var results = new List<UserTenantMemberDto>(members.Count);
        foreach (var member in members)
        {
            results.Add(new UserTenantMemberDto
            {
                UserId = member.UserId,
                Email = member.Email,
                Name = member.Name,
                RoleId = member.RoleId,
                RoleName = member.RoleName,
                Status = member.Status
            });
        }

        return AppServiceResult<IReadOnlyList<UserTenantMemberDto>>.Success(results);
    }

    public async Task<AppServiceResult<bool>> ChangeRoleAsync(Guid memberUserId, Guid roleId, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not Guid actorId || _currentTenant.Id is not Guid tenantId)
        {
            return AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
        }

        AppServiceResult<bool>? result = null;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _memberships.LockTenantAsync(tenantId, ct);
            var actor = await _authorization.GetAccessAsync(actorId, tenantId, ct);
            if (actor is null ||
                !_authorization.HasPermission(actor, PermissionCodes.MemberManage))
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
                return;
            }

            var target = await _memberships.FindAsync(memberUserId, tenantId, ct);
            var newRole = await _roles.FindAsync(tenantId, roleId, ct);
            if (target is null || newRole is null)
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.NotFound);
                return;
            }

            if (target.Status == UserTenantStatus.Revoked)
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.Conflict);
                return;
            }

            var oldRole = await _roles.FindAsync(tenantId, target.RoleId, ct);
            var oldIsOwner = oldRole is not null && IsOwner(oldRole);
            var newIsOwner = IsOwner(newRole);
            if ((oldIsOwner || newIsOwner) && !actor.IsOwner && !actor.IsPlatformAdmin)
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
                return;
            }

            if (!await _authorization.CanGrantRoleAsync(actor, newRole, ct))
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
                return;
            }

            if (oldIsOwner && !newIsOwner &&
                target.Status == UserTenantStatus.Active &&
                await _memberships.CountActiveOwnersAsync(tenantId, ct) <= 1)
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.Conflict);
                return;
            }

            target.RoleId = roleId;
            await _memberships.UpdateAsync(target, ct);
            result = AppServiceResult<bool>.Success(true);
        }, cancellationToken);
        return result!;
    }

    public async Task<AppServiceResult<bool>> RevokeAsync(Guid memberUserId, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not Guid actorId || _currentTenant.Id is not Guid tenantId)
        {
            return AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
        }

        AppServiceResult<bool>? result = null;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _memberships.LockTenantAsync(tenantId, ct);
            var actor = await _authorization.GetAccessAsync(actorId, tenantId, ct);
            if (actor is null ||
                !_authorization.HasPermission(actor, PermissionCodes.MemberManage))
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
                return;
            }

            var target = await _memberships.FindAsync(memberUserId, tenantId, ct);
            if (target is null)
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.NotFound);
                return;
            }

            if (target.Status == UserTenantStatus.Revoked)
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.Conflict);
                return;
            }

            var role = await _roles.FindAsync(tenantId, target.RoleId, ct);
            if (role is not null && IsOwner(role))
            {
                if (!actor.IsOwner && !actor.IsPlatformAdmin)
                {
                    result = AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
                    return;
                }

                if (target.Status == UserTenantStatus.Active &&
                    await _memberships.CountActiveOwnersAsync(tenantId, ct) <= 1)
                {
                    result = AppServiceResult<bool>.Failure(AppServiceError.Conflict);
                    return;
                }
            }

            target.Status = UserTenantStatus.Revoked;
            await _memberships.UpdateAsync(target, ct);
            result = AppServiceResult<bool>.Success(true);
        }, cancellationToken);
        return result!;
    }

    public async Task<AppServiceResult<bool>> ChangeStatusAsync(Guid memberUserId, UserTenantChangeStatusRequestDto request, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not Guid actorId || _currentTenant.Id is not Guid tenantId)
        {
            return AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
        }

        if (request.Status is not (UserTenantStatus.Active or UserTenantStatus.Suspended))
        {
            return AppServiceResult<bool>.Failure(AppServiceError.Invalid);
        }

        AppServiceResult<bool>? result = null;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _memberships.LockTenantAsync(tenantId, ct);
            var actor = await _authorization.GetAccessAsync(actorId, tenantId, ct);
            if (actor is null ||
                !_authorization.HasPermission(actor, PermissionCodes.MemberManage))
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
                return;
            }

            var target = await _memberships.FindAsync(memberUserId, tenantId, ct);
            if (target is null)
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.NotFound);
                return;
            }

            if (target.Status == UserTenantStatus.Revoked)
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.Conflict);
                return;
            }

            if (target.Status == request.Status)
            {
                result = AppServiceResult<bool>.Success(true);
                return;
            }

            var role = await _roles.FindAsync(tenantId, target.RoleId, ct);
            if (role is not null && IsOwner(role))
            {
                if (!actor.IsOwner && !actor.IsPlatformAdmin)
                {
                    result = AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
                    return;
                }

                if (request.Status == UserTenantStatus.Suspended &&
                    await _memberships.CountActiveOwnersAsync(tenantId, ct) <= 1)
                {
                    result = AppServiceResult<bool>.Failure(AppServiceError.Conflict);
                    return;
                }
            }

            target.Status = request.Status;
            await _memberships.UpdateAsync(target, ct);
            result = AppServiceResult<bool>.Success(true);
        }, cancellationToken);
        return result!;
    }

    private static bool IsOwner(Role role)
    {
        return string.Equals(role.Name, "Owner", StringComparison.OrdinalIgnoreCase);
    }
}
