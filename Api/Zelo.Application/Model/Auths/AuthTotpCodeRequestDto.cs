using System.ComponentModel.DataAnnotations;

namespace Zelo.Application.Model.Auths;

public class AuthTotpCodeRequestDto
{
    [Required, StringLength(6, MinimumLength = 6)]
    public required string Code { get; set; }
}
