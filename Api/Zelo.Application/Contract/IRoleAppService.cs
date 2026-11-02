using Zelo.Application.Model.Roles;
using Zelo.Application.Shared.Authorization;

namespace Zelo.Application.Contract;

public interface IRoleAppService
{
    Task<AppServiceResult<IReadOnlyList<RoleDto>>> GetAllAsync(CancellationToken cancellationToken);
    Task<AppServiceResult<RoleDto>> CreateAsync(
        RoleCreateRequestDto request, CancellationToken cancellationToken);
    Task<AppServiceResult<RoleDto>> UpdateAsync(
        Guid roleId, RoleCreateRequestDto request, CancellationToken cancellationToken);
}
