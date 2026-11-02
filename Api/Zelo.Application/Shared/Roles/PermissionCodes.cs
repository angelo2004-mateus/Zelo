namespace Zelo.Application.Shared.Roles;

public static class PermissionCodes
{
    public const string AccountManage = "account.manage";
    public const string CompanyManage = "company.manage";
    public const string MemberManage = "member.manage";
    public const string FinanceRead = "finance.read";
    public const string FinanceManage = "finance.manage";
    public const string FinanceExport = "finance.export";

    public static IReadOnlyList<string> All { get; } =
    [AccountManage, CompanyManage, MemberManage, FinanceRead, FinanceManage, FinanceExport];
}
