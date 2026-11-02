using AutoMapper;
using Zelo.Application.Shared.Authorization;
using Zelo.Application.Shared.Context;
using Zelo.Application.Shared.Data;
using Zelo.Domain.Shared.Entities;
using Zelo.Domain.Shared.Repositories;

namespace Zelo.Application.Shared.Crud;

public abstract class CrudAppService<TEntity, TId, TEntityDto, TGetAllDto, TCreatingDto, TEditingDto> : ICrudAppService<TEntity, TId, TEntityDto, TGetAllDto, TCreatingDto, TEditingDto>
    where TEntity : Entity, IAggregateRoot, new()
{
    private readonly IRepository<TEntity, TId> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;

    protected CrudAppService(IRepository<TEntity, TId> repository, IUnitOfWork unitOfWork, IMapper mapper, ICurrentUser currentUser, ICurrentTenant currentTenant)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
    }

    public virtual async Task<AppServiceResult<TEntityDto>> GetAsync(TId id)
    {
        if (!HasValidScope())
        {
            return AppServiceResult<TEntityDto>.Failure(AppServiceError.Invalid);
        }

        if (_currentUser.Id is null || !await CanReadAsync())
        {
            return AppServiceResult<TEntityDto>.Failure(AppServiceError.Forbidden);
        }

        var entity = await _repository.GetAsync(id);
        return entity is null
            ? AppServiceResult<TEntityDto>.Failure(AppServiceError.NotFound)
            : AppServiceResult<TEntityDto>.Success(_mapper.Map<TEntityDto>(entity));
    }

    public virtual async Task<AppServiceResult<IReadOnlyList<TGetAllDto>>> GetAllAsync()
    {
        if (!HasValidScope())
        {
            return AppServiceResult<IReadOnlyList<TGetAllDto>>.Failure(AppServiceError.Invalid);
        }

        if (_currentUser.Id is null || !await CanReadAsync())
        {
            return AppServiceResult<IReadOnlyList<TGetAllDto>>.Failure(AppServiceError.Forbidden);
        }

        var entities = await _repository.GetAllAsync();
        return AppServiceResult<IReadOnlyList<TGetAllDto>>.Success(entities.Select(_mapper.Map<TGetAllDto>).ToArray());
    }

    public virtual async Task<AppServiceResult<TEntityDto>> CreateAsync(TCreatingDto request)
    {
        if (!HasValidScope() || request is null)
        {
            return AppServiceResult<TEntityDto>.Failure(AppServiceError.Invalid);
        }

        if (_currentUser.Id is null)
        {
            return AppServiceResult<TEntityDto>.Failure(AppServiceError.Forbidden);
        }

        var result = AppServiceResult<TEntityDto>.Failure(AppServiceError.Forbidden);
        await _unitOfWork.ExecuteInTransactionAsync(async cancellationToken =>
        {
            await BeforeWriteAsync(cancellationToken);
            if (!await CanWriteAsync(cancellationToken))
            {
                return;
            }

            var entity = _mapper.Map<TEntity>(request);
            entity.Id = Guid.NewGuid();
            if (entity is IMultiTenant scoped)
            {
                scoped.TenantId = _currentTenant.Id!.Value;
            }

            await OnBeforeCreateOrUpdateAsync(CrudWriteOperation.Create, entity, cancellationToken);
            await _repository.CreateAsync(entity, cancellationToken);
            await OnAfterCreateOrUpdateAsync(CrudWriteOperation.Create, entity, cancellationToken);
            result = AppServiceResult<TEntityDto>.Success(_mapper.Map<TEntityDto>(entity));
        });
        return result;
    }

    public virtual async Task<AppServiceResult<TEntityDto>> UpdateAsync(TId id, TEditingDto request)
    {
        if (!HasValidScope() || request is null)
        {
            return AppServiceResult<TEntityDto>.Failure(AppServiceError.Invalid);
        }

        if (_currentUser.Id is null)
        {
            return AppServiceResult<TEntityDto>.Failure(AppServiceError.Forbidden);
        }

        var result = AppServiceResult<TEntityDto>.Failure(AppServiceError.Forbidden);
        await _unitOfWork.ExecuteInTransactionAsync(async cancellationToken =>
        {
            await BeforeWriteAsync(cancellationToken);
            if (!await CanWriteAsync(cancellationToken))
            {
                return;
            }

            var entity = await _repository.GetAsync(id);
            if (entity is null)
            {
                result = AppServiceResult<TEntityDto>.Failure(AppServiceError.NotFound);
                return;
            }

            _mapper.Map(request, entity);
            await OnBeforeCreateOrUpdateAsync(CrudWriteOperation.Update, entity, cancellationToken);
            await _repository.UpdateAsync(entity, cancellationToken);
            await OnAfterCreateOrUpdateAsync(CrudWriteOperation.Update, entity, cancellationToken);
            result = AppServiceResult<TEntityDto>.Success(_mapper.Map<TEntityDto>(entity));
        });
        return result;
    }

    public virtual async Task<AppServiceResult<bool>> DeleteAsync(TId id)
    {
        if (!HasValidScope())
        {
            return AppServiceResult<bool>.Failure(AppServiceError.Invalid);
        }

        if (_currentUser.Id is null)
        {
            return AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
        }

        var result = AppServiceResult<bool>.Failure(AppServiceError.Forbidden);
        await _unitOfWork.ExecuteInTransactionAsync(async cancellationToken =>
        {
            await BeforeWriteAsync(cancellationToken);
            if (!await CanWriteAsync(cancellationToken))
            {
                return;
            }

            var entity = await _repository.GetAsync(id);
            if (entity is null)
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.NotFound);
                return;
            }

            if (entity is not ISoftDeletable)
            {
                result = AppServiceResult<bool>.Failure(AppServiceError.Conflict);
                return;
            }

            await OnBeforeDeleteAsync(entity, cancellationToken);
            await _repository.DeleteAsync(entity, cancellationToken);
            await OnAfterDeleteAsync(entity, cancellationToken);
            result = AppServiceResult<bool>.Success(true);
        });
        return result;
    }

    protected abstract Task<bool> CanReadAsync(CancellationToken cancellationToken = default);
    protected abstract Task<bool> CanWriteAsync(CancellationToken cancellationToken = default);

    protected virtual Task BeforeWriteAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnBeforeCreateOrUpdateAsync(CrudWriteOperation operation, TEntity entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnAfterCreateOrUpdateAsync(CrudWriteOperation operation, TEntity entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnBeforeDeleteAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnAfterDeleteAsync(TEntity entity, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private bool HasValidScope()
    {
        return !typeof(IMultiTenant).IsAssignableFrom(typeof(TEntity)) || _currentTenant.Id.HasValue;
    }
}
