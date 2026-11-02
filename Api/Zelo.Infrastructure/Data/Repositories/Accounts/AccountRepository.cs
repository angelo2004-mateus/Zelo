using Zelo.Application.Shared.Context;
using NHibernate;
using Zelo.Application.Shared.Data;
using Zelo.Infrastructure.Shared.Data.Repositories;
using NHibernate.Linq;
using Zelo.Domain.Accounts;
using Zelo.Domain.Roles;
using Zelo.Domain.UserTenants;
using Zelo.Domain.Users;

namespace Zelo.Infrastructure.Data.Repositories.Accounts;

public class AccountRepository : Repository<Account, Guid>, IAccountRepository
{
    private readonly ISession _session;

    public AccountRepository(ISession session, IUnitOfWork unitOfWork,
        ICurrentTenant currentTenant, ICurrentUser currentUser)
        : base(session, unitOfWork, currentTenant, currentUser)
    {
        _session = session;
    }

    public async Task AddRoleAsync(Role role, CancellationToken cancellationToken)
    {
        await _session.SaveAsync(role, cancellationToken);
    }

    public async Task AddRolePermissionAsync(RolePermission permission, CancellationToken cancellationToken)
    {
        await _session.SaveAsync(permission, cancellationToken);
    }

    public async Task UpdateRoleAsync(Role role, CancellationToken cancellationToken)
    {
        await _session.UpdateAsync(role, cancellationToken);
    }

    public async Task<IReadOnlyList<RolePermission>> ListRolePermissionEntitiesAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken)
    {
        return await _session.Query<RolePermission>()
            .Where(permission => permission.TenantId == tenantId &&
                                 permission.RoleId == roleId)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteRolePermissionAsync(RolePermission permission, CancellationToken cancellationToken)
    {
        await _session.DeleteAsync(permission, cancellationToken);
    }

    public async Task<IReadOnlyList<AccountAccess>> ListAccessibleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _session.GetAsync<User>(userId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active ||
            user.EmailConfirmationTime is null)
        {
            return [];
        }

        if (user.UserRole == UserRole.Admin)
        {
            var allAccounts = await _session.Query<Account>().ToListAsync(cancellationToken);
            var access = new List<AccountAccess>(allAccounts.Count);

            foreach (var account in allAccounts)
            {
                access.Add(new AccountAccess(
                    account.Id,
                    account.Name,
                    null,
                    null,
                    true,
                    account.Status == AccountStatus.Active));
            }

            return access;
        }

        var memberships = await _session.Query<UserTenant>()
            .Where(member =>
                member.UserId == userId &&
                member.Status == UserTenantStatus.Active)
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
        {
            return [];
        }

        var accountIds = new HashSet<Guid>();
        var roleIds = new HashSet<Guid>();

        foreach (var membership in memberships)
        {
            accountIds.Add(membership.TenantId);
            roleIds.Add(membership.RoleId);
        }

        var accountIdValues = accountIds.ToArray();
        var roleIdValues = roleIds.ToArray();

        var accountRows = await _session.Query<Account>()
            .Where(account =>
                accountIdValues.Contains(account.Id) &&
                account.Status == AccountStatus.Active)
            .ToListAsync(cancellationToken);

        var roleRows = await _session.Query<Role>()
            .Where(role => roleIdValues.Contains(role.Id))
            .ToListAsync(cancellationToken);

        var accounts = new Dictionary<Guid, Account>(accountRows.Count);
        foreach (var account in accountRows)
        {
            accounts.Add(account.Id, account);
        }

        var roles = new Dictionary<Guid, Role>(roleRows.Count);
        foreach (var role in roleRows)
        {
            roles.Add(role.Id, role);
        }

        var results = new List<AccountAccess>();
        foreach (var membership in memberships)
        {
            if (!accounts.TryGetValue(membership.TenantId, out var account) ||
                !roles.TryGetValue(membership.RoleId, out var role) ||
                role.TenantId != membership.TenantId)
            {
                continue;
            }

            results.Add(new AccountAccess(
                membership.TenantId,
                account.Name,
                membership.RoleId,
                role.Name,
                false,
                true));
        }

        return results;
    }

    public async Task<AccountAccess?> GetAccessibleAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var user = await _session.GetAsync<User>(userId, cancellationToken);
        var account = await _session.GetAsync<Account>(tenantId, cancellationToken);
        if (user is null || account is null || user.Status != UserStatus.Active ||
            user.EmailConfirmationTime is null)
        {
            return null;
        }

        if (user.UserRole == UserRole.Admin)
        {
            return new AccountAccess(account.Id, account.Name, null, null, true,
                account.Status == AccountStatus.Active);
        }

        if (account.Status != AccountStatus.Active)
        {
            return null;
        }

        var membership = await _session.Query<UserTenant>().FirstOrDefaultAsync(member =>
            member.UserId == userId && member.TenantId == tenantId &&
            member.Status == UserTenantStatus.Active, cancellationToken);
        if (membership is null)
        {
            return null;
        }

        var role = await _session.Query<Role>().FirstOrDefaultAsync(candidate =>
            candidate.Id == membership.RoleId && candidate.TenantId == tenantId,
            cancellationToken);
        return role is null ? null : new AccountAccess(
            account.Id, account.Name, role.Id, role.Name, false, true);
    }
}
