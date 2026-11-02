using Zelo.Domain.Shared.Entities;

namespace Zelo.Domain.Accounts;

public class Account : AuditedAggregateRoot
{
    public virtual string Name { get; set; }
    public virtual AccountStatus Status { get; set; }

    public Account() { }
}
