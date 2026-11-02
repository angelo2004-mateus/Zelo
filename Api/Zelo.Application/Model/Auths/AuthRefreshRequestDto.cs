using System.ComponentModel.DataAnnotations;

namespace Zelo.Application.Model.Auths;

public class AuthRefreshRequestDto
{
    [Required, MaxLength(128)]
    public required string RefreshToken { get; set; }
}
