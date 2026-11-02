using Zelo.Domain.Shared.Entities;

namespace Zelo.Application.Shared.Crud;

public interface ICrudAppService<TEntity, TId, TEntityDto> : ICrudAppService<TEntity, TId, TEntityDto, TEntityDto>
    where TEntity : class, IAggregateRoot
{
}
