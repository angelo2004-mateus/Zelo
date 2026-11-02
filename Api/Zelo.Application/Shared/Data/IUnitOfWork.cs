using Zelo.Domain.Shared.Entities;

namespace Zelo.Application.Shared.Data;

public interface IUnitOfWork
{
    void Track(IAggregateRoot aggregate);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
