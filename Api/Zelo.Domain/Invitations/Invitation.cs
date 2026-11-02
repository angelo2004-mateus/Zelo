using Zelo.Domain.Shared.Entities;

namespace Zelo.Domain.Invitations;

public class Invitation : AggregateRoot, IMultiTenant
{
    public virtual Guid TenantId { get; set; }
    public virtual string Email { get; set; }
    public virtual Guid RoleId { get; set; }
    public virtual Guid InvitedByUserId { get; set; }
    public virtual InvitationStatus Status { get; set; }
    public virtual string TokenHash { get; set; }
    public virtual DateTime ExpirationTime { get; set; }
    public virtual DateTime? AcceptedTime { get; set; }

    public Invitation() { }
}
