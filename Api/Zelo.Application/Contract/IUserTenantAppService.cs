using Zelo.Application.Model.UserTenants;
using Zelo.Application.Shared.Authorization;

namespace Zelo.Application.Contract;

public interface IUserTenantAppService
{
    Task<AppServiceResult<IReadOnlyList<UserTenantMemberDto>>> GetAllAsync(
        CancellationToken cancellationToken);
    Task<AppServiceResult<bool>> ChangeRoleAsync(
        Guid memberUserId, Guid roleId, CancellationToken cancellationToken);
    Task<AppServiceResult<bool>> RevokeAsync(
        Guid memberUserId, CancellationToken cancellationToken);
    Task<AppServiceResult<bool>> ChangeStatusAsync(
        Guid memberUserId, UserTenantChangeStatusRequestDto request,
        CancellationToken cancellationToken);
}
