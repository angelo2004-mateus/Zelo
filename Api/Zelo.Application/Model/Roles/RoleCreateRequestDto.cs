using System.ComponentModel.DataAnnotations;

namespace Zelo.Application.Model.Roles;

public class RoleCreateRequestDto
{
    [Required]
    public required string Name { get; set; }

    [Required, MinLength(1)]
    public required IReadOnlyList<string> Permissions { get; set; }
}
