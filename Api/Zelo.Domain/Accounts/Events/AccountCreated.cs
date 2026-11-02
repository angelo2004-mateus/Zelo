using Zelo.Domain.Shared.Events;

namespace Zelo.Domain.Accounts.Events;

public sealed record AccountCreated(Guid AccountId, Guid CreatorUserId) : IDomainEvent;
