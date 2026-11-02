using Zelo.Application.Model.Invitations;
using Zelo.Application.Shared.Authorization;

namespace Zelo.Application.Contract;

public interface IInvitationAppService
{
    Task<AppServiceResult<InvitationDto>> CreateOrResendAsync(
        InvitationCreateRequestDto request, CancellationToken cancellationToken);
    Task<AppServiceResult<InvitationAcceptDto>> AcceptAsync(
        string token, CancellationToken cancellationToken);
    Task<AppServiceResult<bool>> RevokeAsync(
        Guid invitationId, CancellationToken cancellationToken);
}
