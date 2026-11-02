using Zelo.Domain.Shared.Events;

namespace Zelo.Domain.Shared.Entities;

public abstract class AggregateRoot : Entity, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents;

    public virtual IReadOnlyCollection<IDomainEvent> DomainEvents
    {
        get
        {
            return _domainEvents.AsReadOnly();
        }
    }

    public AggregateRoot()
    {
        _domainEvents = [];
    }

    public virtual void AddEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public virtual IReadOnlyList<IDomainEvent> PullEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}
