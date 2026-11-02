using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zelo.Application.Contract;

namespace Zelo.Web.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IAccountAppService _accounts;

    public AccountsController(IAccountAppService accounts)
    {
        _accounts = accounts;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _accounts.GetAllAsync(cancellationToken));
    }

    [HttpGet("{tenantId:guid}")]
    public async Task<IActionResult> Get(Guid tenantId, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetAsync(tenantId, cancellationToken);
        return account is null ? NotFound() : Ok(account);
    }
}
