using Zelo.Domain.Shared.Entities;

namespace Zelo.Application.Shared.Crud;

public interface ICrudAppService<TEntity, TId, TEntityDto, TGetAllDto, TEditingDto> : ICrudAppService<TEntity, TId, TEntityDto, TGetAllDto, TEditingDto, TEditingDto>
    where TEntity : class, IAggregateRoot
{
}
