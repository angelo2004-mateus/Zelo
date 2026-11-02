using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zelo.Application.Contract;
using Zelo.Application.Model.UserTenants;
using Zelo.Web.Api.Shared.Authorization;

namespace Zelo.Web.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts/{tenantId:guid}/members")]
public class MembersController : ControllerBase
{
    private readonly IUserTenantAppService _members;

    public MembersController(IUserTenantAppService members)
    {
        _members = members;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return AppServiceResultHttpMapper.Map(this, await _members.GetAllAsync(cancellationToken));
    }

    [HttpPatch("{memberUserId:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid memberUserId,
        UserTenantChangeRoleRequestDto request, CancellationToken cancellationToken)
    {
        return AppServiceResultHttpMapper.Map(this,
            await _members.ChangeRoleAsync(memberUserId, request.RoleId, cancellationToken),
            StatusCodes.Status204NoContent);
    }

    [HttpDelete("{memberUserId:guid}")]
    public async Task<IActionResult> Revoke(Guid memberUserId, CancellationToken cancellationToken)
    {
        return AppServiceResultHttpMapper.Map(this,
            await _members.RevokeAsync(memberUserId, cancellationToken), StatusCodes.Status204NoContent);
    }

    [HttpPatch("{memberUserId:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid memberUserId,
        UserTenantChangeStatusRequestDto request, CancellationToken cancellationToken)
    {
        return AppServiceResultHttpMapper.Map(this,
            await _members.ChangeStatusAsync(memberUserId, request, cancellationToken),
            StatusCodes.Status204NoContent);
    }
}
