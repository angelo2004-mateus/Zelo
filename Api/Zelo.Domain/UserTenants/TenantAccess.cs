namespace Zelo.Domain.UserTenants;

public class TenantAccess
{
    public Guid TenantId { get; }
    public Guid UserId { get; }
    public bool IsPlatformAdmin { get; }
    public bool IsOwner { get; }
    public Guid? RoleId { get; }
    public IReadOnlyList<string> Permissions { get; }

    public TenantAccess(Guid tenantId, Guid userId, bool isPlatformAdmin, bool isOwner, Guid? roleId, IReadOnlyList<string> permissions)
    {
        TenantId = tenantId;
        UserId = userId;
        IsPlatformAdmin = isPlatformAdmin;
        IsOwner = isOwner;
        RoleId = roleId;
        Permissions = permissions;
    }
}
