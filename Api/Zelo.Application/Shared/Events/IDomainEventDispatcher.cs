using Zelo.Domain.Shared.Events;

namespace Zelo.Application.Shared.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
