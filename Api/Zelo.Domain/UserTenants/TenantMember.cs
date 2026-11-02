namespace Zelo.Domain.UserTenants;

public class TenantMember
{
    public Guid UserId { get; }
    public string Email { get; }
    public string Name { get; }
    public Guid RoleId { get; }
    public string RoleName { get; }
    public UserTenantStatus Status { get; }

    public TenantMember(Guid userId, string email, string name, Guid roleId, string roleName, UserTenantStatus status)
    {
        UserId = userId;
        Email = email;
        Name = name;
        RoleId = roleId;
        RoleName = roleName;
        Status = status;
    }
}
