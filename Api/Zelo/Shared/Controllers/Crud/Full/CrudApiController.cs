using Microsoft.AspNetCore.Mvc;
using Zelo.Application.Shared.Crud;
using Zelo.Domain.Shared.Entities;
using Zelo.Web.Api.Shared.Authorization;

namespace Zelo.Web.Api.Shared.Controllers;

public abstract class CrudApiController<TEntity, TId, TEntityDto, TGetAllDto, TCreatingDto, TEditingDto, TIAppService> : ControllerBase
    where TEntity : class, IAggregateRoot
    where TIAppService : ICrudAppService<TEntity, TId, TEntityDto, TGetAllDto, TCreatingDto, TEditingDto>
{
    protected TIAppService AppService { get; }

    protected CrudApiController(TIAppService appService)
    {
        AppService = appService;
    }

    protected async Task<IActionResult> GetAsync(TId id)
    {
        return AppServiceResultHttpMapper.Map(this, await AppService.GetAsync(id));
    }

    protected async Task<IActionResult> GetAllAsync()
    {
        return AppServiceResultHttpMapper.Map(this, await AppService.GetAllAsync());
    }

    protected async Task<IActionResult> CreateAsync(TCreatingDto request)
    {
        return AppServiceResultHttpMapper.Map(
            this, await AppService.CreateAsync(request), StatusCodes.Status201Created);
    }

    protected async Task<IActionResult> UpdateAsync(TId id, TEditingDto request)
    {
        return AppServiceResultHttpMapper.Map(this, await AppService.UpdateAsync(id, request));
    }

    protected async Task<IActionResult> DeleteAsync(TId id)
    {
        return AppServiceResultHttpMapper.Map(
            this, await AppService.DeleteAsync(id), StatusCodes.Status204NoContent);
    }
}
