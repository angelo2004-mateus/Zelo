using System.IdentityModel.Tokens.Jwt;
using Zelo.Application.Shared.Context;

namespace Zelo.Web.Api.Shared.Context;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public Guid? Id
    {
        get
        {
            var value = _accessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }
}
