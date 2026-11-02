using Zelo.Domain.Shared.Entities;

namespace Zelo.Domain.Users;

public class UserTotpCredential : Entity
{
    public virtual Guid UserId { get; set; }
    public virtual string ProtectedSecret { get; set; }
    public virtual DateTime? EnabledTime { get; set; }
    public virtual long? LastUsedStep { get; set; }
    public virtual int FailedAttemptCount { get; set; }
    public virtual DateTime? LockoutEndTime { get; set; }

    public UserTotpCredential() { }
}
