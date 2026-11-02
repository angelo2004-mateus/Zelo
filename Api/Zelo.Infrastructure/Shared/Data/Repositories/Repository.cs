using System.Linq.Expressions;
using NHibernate;
using NHibernate.Linq;
using Zelo.Application.Shared.Context;
using Zelo.Application.Shared.Data;
using Zelo.Domain.Shared.Entities;
using Zelo.Domain.Shared.Repositories;

namespace Zelo.Infrastructure.Shared.Data.Repositories;

public class Repository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : class, IAggregateRoot
{
    private readonly ISession _session;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;

    public Repository(ISession session, IUnitOfWork unitOfWork,
        ICurrentTenant currentTenant, ICurrentUser currentUser)
    {
        _session = session;
        _unitOfWork = unitOfWork;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
    }

    public async Task<TEntity?> GetAsync(TId id)
    {
        RequireTenantForRead();
        if (id is not Guid entityId)
        {
            throw new NotSupportedException("Zelo entities use Guid identifiers.");
        }

        return await ActiveQuery().FirstOrDefaultAsync(entity => entity.Id == entityId);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync()
    {
        RequireTenantForRead();
        return await ActiveQuery().ToListAsync();
    }

    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        RequireMatchingTenant(entity);
        await OnCreateAsync(entity, cancellationToken);
        await _session.SaveAsync(entity, cancellationToken);
        _unitOfWork.Track(entity);
        return entity;
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        RequireMatchingTenant(entity);
        await OnUpdateAsync(entity, cancellationToken);
        await _session.UpdateAsync(entity, cancellationToken);
        _unitOfWork.Track(entity);
        return entity;
    }

    public async Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        RequireMatchingTenant(entity);
        if (entity is not ISoftDeletable)
        {
            throw new NotSupportedException("This entity does not support soft delete.");
        }

        await OnDeleteAsync(entity, cancellationToken);
        await UpdateAsync(entity, cancellationToken);
    }

    protected virtual Task OnCreateAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnUpdateAsync(TEntity entity, CancellationToken cancellationToken) => Task.CompletedTask;

    protected virtual Task OnDeleteAsync(TEntity entity, CancellationToken cancellationToken)
    {
        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.IsDeleted = true;
            softDeletable.DeletionTime = DateTime.UtcNow;
            softDeletable.DeleterId = _currentUser.Id;
        }

        return Task.CompletedTask;
    }

    private void RequireTenantForRead()
    {
        if (typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)) && _currentTenant.Id is null)
        {
            throw new InvalidOperationException("A current tenant is required for this query.");
        }
    }

    private void RequireMatchingTenant(TEntity entity)
    {
        if (entity is IMultiTenant tenantOwned && _currentTenant.Id is Guid tenantId &&
            tenantOwned.TenantId != tenantId)
        {
            throw new InvalidOperationException("Entity belongs to a different tenant.");
        }
    }

    private IQueryable<TEntity> ActiveQuery()
    {
        var query = _session.Query<TEntity>();
        if (!typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity)))
        {
            return query;
        }

        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var visible = Expression.Not(Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)));
        return query.Where(Expression.Lambda<Func<TEntity, bool>>(visible, parameter));
    }
}
