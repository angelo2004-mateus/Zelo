using System.ComponentModel.DataAnnotations;

namespace Zelo.Application.Model.Auths;

public class AuthTokenRequestDto
{
    [Required, MaxLength(128)]
    public required string Token { get; set; }
}
