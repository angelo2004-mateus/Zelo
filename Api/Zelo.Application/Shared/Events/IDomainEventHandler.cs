using Zelo.Domain.Shared.Events;

namespace Zelo.Application.Shared.Events;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
