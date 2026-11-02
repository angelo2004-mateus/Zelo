using Zelo.Domain.Shared.Events;

namespace Zelo.Domain.Shared.Entities;

public interface IAggregateRoot : IEntity
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void AddEvent(IDomainEvent domainEvent);
    IReadOnlyList<IDomainEvent> PullEvents();
}
