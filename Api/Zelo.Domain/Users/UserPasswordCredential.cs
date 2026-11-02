using Zelo.Domain.Shared.Entities;

namespace Zelo.Domain.Users;

public class UserPasswordCredential : Entity
{
    public virtual Guid UserId { get; set; }
    public virtual string PasswordHash { get; set; }
    public virtual int FailedLoginCount { get; set; }
    public virtual DateTime? LockoutEndTime { get; set; }

    public UserPasswordCredential() { }
}
