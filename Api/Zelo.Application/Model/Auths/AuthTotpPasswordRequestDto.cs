using System.ComponentModel.DataAnnotations;

namespace Zelo.Application.Model.Auths;

public class AuthTotpPasswordRequestDto
{
    [Required]
    public required string Password { get; set; }
}
