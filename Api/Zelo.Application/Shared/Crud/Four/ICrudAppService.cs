using Zelo.Domain.Shared.Entities;

namespace Zelo.Application.Shared.Crud;

public interface ICrudAppService<TEntity, TId, TEntityDto, TEditingDto> : ICrudAppService<TEntity, TId, TEntityDto, TEntityDto, TEditingDto>
    where TEntity : class, IAggregateRoot
{
}
