using Zelo.Domain.Shared.Entities;

namespace Zelo.Domain.UserTenants;

public class UserTenant : AuditedAggregateRoot, IMultiTenant
{
    public virtual Guid UserId { get; set; }
    public virtual Guid TenantId { get; set; }
    public virtual Guid RoleId { get; set; }
    public virtual UserTenantStatus Status { get; set; }
    public virtual DateTime JoinTime { get; set; }

    public UserTenant() { }
}
