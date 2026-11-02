using Microsoft.AspNetCore.Mvc;
using Zelo.Application.Shared.Authorization;

namespace Zelo.Web.Api.Shared.Authorization;

public static class AppServiceResultHttpMapper
{
    public static IActionResult Map<T>(ControllerBase controller, AppServiceResult<T> result, int successStatus = StatusCodes.Status200OK)
    {
        if (result.Succeeded)
        {
            if (successStatus == StatusCodes.Status204NoContent)
            {
                return controller.NoContent();
            }

            return controller.StatusCode(successStatus, result.Value);
        }

        switch (result.Error)
        {
            case AppServiceError.Forbidden:
                return controller.Forbid();
            case AppServiceError.Invalid:
                return controller.BadRequest();
            case AppServiceError.Conflict:
                return controller.Conflict();
            case AppServiceError.NotFound:
                return controller.NotFound();
            default:
                return controller.StatusCode(
                    StatusCodes.Status500InternalServerError);
        }
    }
}
