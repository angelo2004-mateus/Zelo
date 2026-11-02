using Zelo.Domain.Shared.Repositories;

namespace Zelo.Domain.Invitations;

public interface IInvitationRepository : IRepository<Invitation, Guid>
{
    Task<Invitation?> FindPendingAsync(
        Guid tenantId, string email, CancellationToken cancellationToken);
    Task<Invitation?> FindByTokenHashAsync(string hash, CancellationToken cancellationToken);
    Task<bool> TryAcceptAsync(Guid invitationId, DateTime now, CancellationToken cancellationToken);
    Task<bool> TryExpireAsync(Guid invitationId, DateTime now, CancellationToken cancellationToken);
    Task<bool> TryRevokeAsync(
        Guid tenantId, Guid invitationId, DateTime now, CancellationToken cancellationToken);
}
