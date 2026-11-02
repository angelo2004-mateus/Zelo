using Zelo.Application.Shared.Context;

namespace Zelo.Web.Api.Shared.Context;

public sealed class HttpCurrentTenant : ICurrentTenant
{
    private readonly IHttpContextAccessor _accessor;

    public Guid? Id
    {
        get
        {
            var value = _accessor.HttpContext?.Request.RouteValues["tenantId"]?.ToString();
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public HttpCurrentTenant(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }
}
