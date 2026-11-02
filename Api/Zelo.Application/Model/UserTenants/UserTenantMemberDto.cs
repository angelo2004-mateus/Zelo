using Zelo.Domain.UserTenants;

namespace Zelo.Application.Model.UserTenants;

public class UserTenantMemberDto
{
    public Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
    public Guid RoleId { get; set; }
    public required string RoleName { get; set; }
    public UserTenantStatus Status { get; set; }
}
