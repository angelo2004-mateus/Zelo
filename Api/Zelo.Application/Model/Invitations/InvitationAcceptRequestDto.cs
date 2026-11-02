using System.ComponentModel.DataAnnotations;

namespace Zelo.Application.Model.Invitations;

public class InvitationAcceptRequestDto
{
    [Required]
    public required string Token { get; set; }
}
