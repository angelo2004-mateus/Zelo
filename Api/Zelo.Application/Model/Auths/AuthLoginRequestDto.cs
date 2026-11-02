using System.ComponentModel.DataAnnotations;

namespace Zelo.Application.Model.Auths;

public class AuthLoginRequestDto
{
    [Required, EmailAddress, MaxLength(320)]
    public required string Email { get; set; }

    [Required, MaxLength(1024)]
    public required string Password { get; set; }
}
