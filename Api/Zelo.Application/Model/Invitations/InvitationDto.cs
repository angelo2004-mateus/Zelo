using Zelo.Domain.Invitations;

namespace Zelo.Application.Model.Invitations;

public class InvitationDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Email { get; set; }
    public Guid RoleId { get; set; }
    public InvitationStatus Status { get; set; }
    public DateTime ExpirationTime { get; set; }
    public bool EmailSent { get; set; }
}
