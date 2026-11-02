using Zelo.Application.Accounts.Services;
using Zelo.Application.Contract;
using Zelo.Application.Model.Roles;
using Zelo.Application.Shared.Authorization;
using Zelo.Application.Shared.Context;

namespace Zelo.Application.Services;

public class RoleAppService : IRoleAppService
{
    private readonly AccountRoleManagementService _roles;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;

    public RoleAppService(AccountRoleManagementService roles,
        ICurrentUser currentUser, ICurrentTenant currentTenant)
    {
        _roles = roles;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
    }

    public Task<AppServiceResult<IReadOnlyList<RoleDto>>> GetAllAsync(CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not Guid userId || _currentTenant.Id is not Guid tenantId)
        {
            return Task.FromResult(AppServiceResult<IReadOnlyList<RoleDto>>.Failure(
                AppServiceError.Forbidden));
        }

        return _roles.ListAsync(userId, tenantId, cancellationToken);
    }

    public Task<AppServiceResult<RoleDto>> CreateAsync(RoleCreateRequestDto request, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not Guid userId || _currentTenant.Id is not Guid tenantId)
        {
            return Task.FromResult(AppServiceResult<RoleDto>.Failure(AppServiceError.Forbidden));
        }

        return _roles.CreateAsync(userId, tenantId, request, cancellationToken);
    }

    public Task<AppServiceResult<RoleDto>> UpdateAsync(Guid roleId, RoleCreateRequestDto request, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not Guid userId || _currentTenant.Id is not Guid tenantId)
        {
            return Task.FromResult(AppServiceResult<RoleDto>.Failure(AppServiceError.Forbidden));
        }

        return _roles.UpdateAsync(userId, tenantId, roleId, request, cancellationToken);
    }
}
