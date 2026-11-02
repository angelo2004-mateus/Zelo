using Zelo.Application.Model.Accounts;
using Zelo.Application.Model.Users;
using Zelo.Application.Shared.Auths.Jwt;

namespace Zelo.Application.Model.Auths;

public class AuthResultDto : JwtBearerAuthResult
{
    public UserDto? User { get; set; }
    public IReadOnlyList<AccountAccessDto>? Accounts { get; set; }
    public bool RequiresTwoFactor { get; set; }
    public string? ChallengeToken { get; set; }
}
