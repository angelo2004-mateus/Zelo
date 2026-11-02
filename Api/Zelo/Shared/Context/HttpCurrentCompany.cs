using Zelo.Application.Shared.Context;

namespace Zelo.Web.Api.Shared.Context;

public sealed class HttpCurrentCompany : ICurrentCompany
{
    private readonly IHttpContextAccessor _accessor;

    public Guid? Id
    {
        get
        {
            var value = _accessor.HttpContext?.Request.RouteValues["companyId"]?.ToString();
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public HttpCurrentCompany(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }
}
