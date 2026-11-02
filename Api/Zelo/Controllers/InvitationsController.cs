using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Zelo.Application.Contract;
using Zelo.Application.Model.Invitations;
using Zelo.Web.Api.Shared.Authorization;

namespace Zelo.Web.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationAppService _invitations;
    private readonly ILogger<InvitationsController> _logger;

    public InvitationsController(IInvitationAppService invitations, ILogger<InvitationsController> logger)
    {
        _invitations = invitations;
        _logger = logger;
    }

    [EnableRateLimiting("auth")]
    [HttpPost("accounts/{tenantId:guid}/invitations")]
    public async Task<IActionResult> Create(InvitationCreateRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _invitations.CreateOrResendAsync(request, cancellationToken);
        if (result.Succeeded && result.Value is { EmailSent: false })
        {
            _logger.LogWarning("Invitation email delivery failed for invitation {InvitationId}.",
                result.Value.Id);
        }

        return AppServiceResultHttpMapper.Map(this, result, StatusCodes.Status201Created);
    }

    [HttpPost("accounts/{tenantId:guid}/invitations/{invitationId:guid}/revoke")]
    public async Task<IActionResult> Revoke(Guid invitationId, CancellationToken cancellationToken)
    {
        return AppServiceResultHttpMapper.Map(this,
            await _invitations.RevokeAsync(invitationId, cancellationToken),
            StatusCodes.Status204NoContent);
    }

    [EnableRateLimiting("auth")]
    [HttpPost("invitations/accept")]
    public async Task<IActionResult> Accept(InvitationAcceptRequestDto request, CancellationToken cancellationToken)
    {
        return AppServiceResultHttpMapper.Map(this,
            await _invitations.AcceptAsync(request.Token, cancellationToken));
    }
}
