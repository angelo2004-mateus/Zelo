using Zelo.Domain.Shared.Entities;

namespace Zelo.Domain.Users;

public class UserAuthSession : Entity
{
    public virtual Guid UserId { get; set; }
    public virtual string RefreshTokenHash { get; set; }
    public virtual DateTime ExpirationTime { get; set; }
    public virtual DateTime? RevocationTime { get; set; }

    public UserAuthSession() { }
}
