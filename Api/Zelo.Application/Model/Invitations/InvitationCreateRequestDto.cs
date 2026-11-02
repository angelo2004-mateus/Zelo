using System.ComponentModel.DataAnnotations;

namespace Zelo.Application.Model.Invitations;

public class InvitationCreateRequestDto
{
    [Required, EmailAddress, MaxLength(320)]
    public required string Email { get; set; }

    public Guid RoleId { get; set; }
}
