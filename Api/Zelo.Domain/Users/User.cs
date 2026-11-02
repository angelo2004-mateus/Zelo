using Zelo.Domain.Shared.Entities;

namespace Zelo.Domain.Users;

public class User : AggregateRoot
{
    public virtual string Email { get; set; }
    public virtual string Name { get; set; }
    public virtual UserRole UserRole { get; set; }
    public virtual UserStatus Status { get; set; }
    public virtual DateTime? EmailConfirmationTime { get; set; }

    public User() { }
}
