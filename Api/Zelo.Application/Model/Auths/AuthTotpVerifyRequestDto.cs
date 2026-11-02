using System.ComponentModel.DataAnnotations;

namespace Zelo.Application.Model.Auths;

public class AuthTotpVerifyRequestDto
{
    [Required, MaxLength(128)]
    public required string ChallengeToken { get; set; }

    [Required, MaxLength(64)]
    public required string Code { get; set; }
}
