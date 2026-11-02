using System.ComponentModel.DataAnnotations;

namespace Zelo.Application.Model.Auths;

public class AuthTotpDisableRequestDto
{
    [Required]
    public required string Password { get; set; }

    [Required, MaxLength(64)]
    public required string Code { get; set; }
}
