using Zelo.Domain.Shared.Entities;

namespace Zelo.Domain.Users;

public class UserAuthToken : Entity
{
    public virtual Guid UserId { get; set; }
    public virtual string TokenHash { get; set; }
    public virtual UserAuthTokenPurpose Purpose { get; set; }
    public virtual DateTime ExpirationTime { get; set; }
    public virtual DateTime? ConsumptionTime { get; set; }
    public virtual int FailedAttemptCount { get; set; }

    public UserAuthToken() { }
}
