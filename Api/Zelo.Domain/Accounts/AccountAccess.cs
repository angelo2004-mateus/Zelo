namespace Zelo.Domain.Accounts;

public class AccountAccess
{
    public Guid AccountId { get; }
    public string AccountName { get; }
    public Guid? RoleId { get; }
    public string? RoleName { get; }
    public bool IsPlatformAdminAccess { get; }
    public bool IsActive { get; }

    public AccountAccess(Guid accountId, string accountName, Guid? roleId, string? roleName, bool isPlatformAdminAccess, bool isActive)
    {
        AccountId = accountId;
        AccountName = accountName;
        RoleId = roleId;
        RoleName = roleName;
        IsPlatformAdminAccess = isPlatformAdminAccess;
        IsActive = isActive;
    }
}
