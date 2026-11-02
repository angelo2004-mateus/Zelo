using Zelo.Application.Shared.Context;
using NHibernate;
using Zelo.Application.Shared.Data;
using Zelo.Infrastructure.Shared.Data.Repositories;
using NHibernate.Linq;
using Zelo.Domain.Invitations;

namespace Zelo.Infrastructure.Data.Repositories.Invitations;

public class InvitationRepository : Repository<Invitation, Guid>, IInvitationRepository
{
    private readonly ISession _session;

    public InvitationRepository(ISession session, IUnitOfWork unitOfWork,
        ICurrentTenant currentTenant, ICurrentUser currentUser)
        : base(session, unitOfWork, currentTenant, currentUser)
    {
        _session = session;
    }

    public async Task<Invitation?> FindPendingAsync(Guid tenantId, string email, CancellationToken cancellationToken)
    {
        return await _session.Query<Invitation>().FirstOrDefaultAsync(
            invitation => invitation.TenantId == tenantId &&
                          invitation.Email.ToLower() == email.ToLower() &&
                          invitation.Status == InvitationStatus.Pending,
            cancellationToken);
    }

    public async Task<Invitation?> FindByTokenHashAsync(string hash, CancellationToken cancellationToken)
    {
        return await _session.Query<Invitation>().FirstOrDefaultAsync(
            invitation => invitation.TokenHash == hash, cancellationToken);
    }

    public async Task<bool> TryAcceptAsync(Guid invitationId, DateTime now, CancellationToken cancellationToken)
    {
        var count = await _session.CreateSQLQuery(
                "UPDATE invitations SET status = :accepted, acceptedtime = :now, " +
                "lastmodifiedtime = :now WHERE id = :id AND status = :pending " +
                "AND expirationtime > :now")
            .SetParameter("accepted", (int)InvitationStatus.Accepted)
            .SetParameter("pending", (int)InvitationStatus.Pending)
            .SetParameter("now", now)
            .SetParameter("id", invitationId)
            .ExecuteUpdateAsync(cancellationToken);
        return count == 1;
    }

    public async Task<bool> TryRevokeAsync(Guid tenantId, Guid invitationId, DateTime now, CancellationToken cancellationToken)
    {
        var count = await _session.CreateSQLQuery(
                "UPDATE invitations SET status = :revoked, lastmodifiedtime = :now " +
                "WHERE id = :id AND tenantid = :tenantid AND status = :pending")
            .SetParameter("revoked", (int)InvitationStatus.Revoked)
            .SetParameter("pending", (int)InvitationStatus.Pending)
            .SetParameter("now", now)
            .SetParameter("id", invitationId)
            .SetParameter("tenantid", tenantId)
            .ExecuteUpdateAsync(cancellationToken);
        return count == 1;
    }

    public async Task<bool> TryExpireAsync(Guid invitationId, DateTime now, CancellationToken cancellationToken)
    {
        var count = await _session.CreateSQLQuery(
                "UPDATE invitations SET status = :expired, lastmodifiedtime = :now " +
                "WHERE id = :id AND status = :pending AND expirationtime <= :now")
            .SetParameter("expired", (int)InvitationStatus.Expired)
            .SetParameter("pending", (int)InvitationStatus.Pending)
            .SetParameter("now", now)
            .SetParameter("id", invitationId)
            .ExecuteUpdateAsync(cancellationToken);
        return count == 1;
    }
}
