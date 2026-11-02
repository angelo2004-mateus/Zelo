using System.ComponentModel.DataAnnotations;

namespace Zelo.Application.Model.Auths;

public class AuthEmailRequestDto
{
    [Required, EmailAddress, MaxLength(320)]
    public required string Email { get; set; }
}
