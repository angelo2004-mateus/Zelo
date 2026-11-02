using Zelo.Application.Shared.Authorization;
using Zelo.Domain.Shared.Entities;

namespace Zelo.Application.Shared.Crud;

public interface ICrudAppService<TEntity, TId, TEntityDto, TGetAllDto, TCreatingDto, TEditingDto>
    where TEntity : class, IAggregateRoot
{
    Task<AppServiceResult<TEntityDto>> GetAsync(TId id);
    Task<AppServiceResult<IReadOnlyList<TGetAllDto>>> GetAllAsync();
    Task<AppServiceResult<TEntityDto>> CreateAsync(TCreatingDto request);
    Task<AppServiceResult<TEntityDto>> UpdateAsync(TId id, TEditingDto request);
    Task<AppServiceResult<bool>> DeleteAsync(TId id);
}
