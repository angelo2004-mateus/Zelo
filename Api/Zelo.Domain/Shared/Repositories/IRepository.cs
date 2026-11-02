using Zelo.Domain.Shared.Entities;

namespace Zelo.Domain.Shared.Repositories;

public interface IRepository<TEntity, TId> where TEntity : class, IAggregateRoot
{
    Task<TEntity?> GetAsync(TId id);
    Task<IReadOnlyList<TEntity>> GetAllAsync();
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default);
}
