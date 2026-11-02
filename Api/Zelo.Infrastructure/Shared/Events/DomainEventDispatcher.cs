using Microsoft.Extensions.DependencyInjection;
using Zelo.Application.Shared.Events;
using Zelo.Domain.Shared.Events;

namespace Zelo.Infrastructure.Shared.Events;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _services;

    public DomainEventDispatcher(IServiceProvider services)
    {
        _services = services;
    }

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return DispatchTypedAsync((dynamic)domainEvent, cancellationToken);
    }

    private async Task DispatchTypedAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        var handlers = _services.GetServices<IDomainEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }
}
