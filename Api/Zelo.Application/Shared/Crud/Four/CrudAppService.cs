using AutoMapper;
using Zelo.Application.Shared.Context;
using Zelo.Application.Shared.Data;
using Zelo.Domain.Shared.Entities;
using Zelo.Domain.Shared.Repositories;

namespace Zelo.Application.Shared.Crud;

public abstract class CrudAppService<TEntity, TId, TEntityDto, TEditingDto> : CrudAppService<TEntity, TId, TEntityDto, TEntityDto, TEditingDto>, ICrudAppService<TEntity, TId, TEntityDto, TEditingDto>
    where TEntity : Entity, IAggregateRoot, new()
{
    protected CrudAppService(IRepository<TEntity, TId> repository, IUnitOfWork unitOfWork, IMapper mapper, ICurrentUser currentUser, ICurrentTenant currentTenant)
        : base(repository, unitOfWork, mapper, currentUser, currentTenant)
    {
    }
}
