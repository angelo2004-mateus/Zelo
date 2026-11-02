using System.ComponentModel.DataAnnotations;

namespace Zelo.Application.Model.Auths;

public class AuthRegisterRequestDto
{
    [Required, EmailAddress, MaxLength(320)]
    public required string Email { get; set; }

    [Required, MinLength(8), MaxLength(1024)]
    public required string Password { get; set; }

    [Required, MaxLength(200)]
    public required string Name { get; set; }

    [Required, MaxLength(200)]
    public required string AccountName { get; set; }
}
