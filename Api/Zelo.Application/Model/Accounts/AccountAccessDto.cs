namespace Zelo.Application.Model.Accounts;

public class AccountAccessDto
{
    public Guid AccountId { get; set; }
    public required string AccountName { get; set; }
    public Guid? RoleId { get; set; }
    public string? RoleName { get; set; }
    public bool IsPlatformAdminAccess { get; set; }
    public bool IsActive { get; set; }
}
