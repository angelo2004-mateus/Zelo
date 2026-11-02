using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zelo.Application.Contract;
using Zelo.Application.Model.Roles;
using Zelo.Web.Api.Shared.Authorization;

namespace Zelo.Web.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts/{tenantId:guid}/roles")]
public class RolesController : ControllerBase
{
    private readonly IRoleAppService _roles;

    public RolesController(IRoleAppService roles)
    {
        _roles = roles;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return AppServiceResultHttpMapper.Map(this, await _roles.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create(RoleCreateRequestDto request, CancellationToken cancellationToken)
    {
        return AppServiceResultHttpMapper.Map(
            this, await _roles.CreateAsync(request, cancellationToken), StatusCodes.Status201Created);
    }

    [HttpPut("{roleId:guid}")]
    public async Task<IActionResult> Update(Guid roleId, RoleCreateRequestDto request, CancellationToken cancellationToken)
    {
        return AppServiceResultHttpMapper.Map(
            this, await _roles.UpdateAsync(roleId, request, cancellationToken));
    }
}
