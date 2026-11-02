using NHibernate;
using Zelo.Application.Shared.Data;
using Zelo.Application.Shared.Events;
using Zelo.Domain.Shared.Entities;

namespace Zelo.Infrastructure.Shared.Data;

public class NHibernateUnitOfWork : IUnitOfWork
{
    private readonly ISession _session;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly HashSet<IAggregateRoot> _tracked = new(ReferenceEqualityComparer.Instance);

    public NHibernateUnitOfWork(ISession session, IDomainEventDispatcher dispatcher)
    {
        _session = session;
        _dispatcher = dispatcher;
    }

    public void Track(IAggregateRoot aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        _tracked.Add(aggregate);
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var transaction = _session.BeginTransaction();
        try
        {
            await action(cancellationToken);
            await DispatchPendingEventsAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (transaction.IsActive)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            throw;
        }
        finally
        {
            _tracked.Clear();
        }
    }

    private async Task DispatchPendingEventsAsync(CancellationToken cancellationToken)
    {
        for (var pass = 0; pass < 1000; pass++)
        {
            var pending = new List<Zelo.Domain.Shared.Events.IDomainEvent>();
            foreach (var aggregate in _tracked)
            {
                pending.AddRange(aggregate.PullEvents());
            }

            if (pending.Count == 0)
            {
                return;
            }

            await _session.FlushAsync(cancellationToken);
            foreach (var domainEvent in pending)
            {
                await _dispatcher.DispatchAsync(domainEvent, cancellationToken);
            }
        }

        throw new InvalidOperationException("Domain event dispatch limit exceeded.");
    }
}
