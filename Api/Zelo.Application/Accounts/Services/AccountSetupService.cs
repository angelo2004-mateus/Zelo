using Zelo.Application.Shared.Roles;
using Zelo.Domain.UserTenants;

namespace Zelo.Application.Accounts.Services;

public class AccountSetupService
{
    private readonly AccountRoleService _roles;
    private readonly IUserTenantRepository _memberships;

    public AccountSetupService(AccountRoleService roles, IUserTenantRepository memberships)
    {
        _roles = roles;
        _memberships = memberships;
    }

    public async Task InitializeAsync(Guid accountId, Guid creatorUserId, CancellationToken cancellationToken)
    {
        var owner = await _roles.CreateAsync(
            accountId, "Owner", PermissionCodes.All, cancellationToken);

        await _roles.CreateAsync(
            accountId,
            "Administrator",
            [
                PermissionCodes.CompanyManage,
                PermissionCodes.MemberManage,
                PermissionCodes.FinanceRead,
                PermissionCodes.FinanceManage,
                PermissionCodes.FinanceExport
            ],
            cancellationToken);

        await _roles.CreateAsync(
            accountId, "Accountant",
            [PermissionCodes.FinanceRead, PermissionCodes.FinanceExport],
            cancellationToken);

        await _roles.CreateAsync(
            accountId, "ReadOnly", [PermissionCodes.FinanceRead],
            cancellationToken);

        var now = DateTime.UtcNow;
        await _memberships.CreateAsync(new UserTenant
        {
            Id = Guid.NewGuid(),
            UserId = creatorUserId,
            TenantId = accountId,
            RoleId = owner.Id,
            Status = UserTenantStatus.Active,
            JoinTime = now,
            CreatorId = creatorUserId
        }, cancellationToken);
    }
}
