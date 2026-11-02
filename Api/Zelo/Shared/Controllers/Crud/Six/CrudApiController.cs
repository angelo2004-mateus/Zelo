using Zelo.Application.Shared.Crud;
using Zelo.Domain.Shared.Entities;

namespace Zelo.Web.Api.Shared.Controllers;

public abstract class CrudApiController<TEntity, TId, TEntityDto, TGetAllDto, TEditingDto, TIAppService> : CrudApiController<TEntity, TId, TEntityDto, TGetAllDto, TEditingDto, TEditingDto, TIAppService>
    where TEntity : class, IAggregateRoot
    where TIAppService : ICrudAppService<TEntity, TId, TEntityDto, TGetAllDto, TEditingDto>
{
    protected CrudApiController(TIAppService appService) : base(appService)
    {
    }
}
