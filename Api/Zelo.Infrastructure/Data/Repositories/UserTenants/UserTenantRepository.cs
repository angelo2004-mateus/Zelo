using Zelo.Application.Shared.Context;
using NHibernate;
using Zelo.Application.Shared.Data;
using Zelo.Infrastructure.Shared.Data.Repositories;
using NHibernate.Linq;
using Zelo.Domain.Accounts;
using Zelo.Domain.Roles;
using Zelo.Domain.UserTenants;
using Zelo.Domain.Users;

namespace Zelo.Infrastructure.Data.Repositories.UserTenants;

public class UserTenantRepository : Repository<UserTenant, Guid>, IUserTenantRepository
{
    private readonly ISession _session;

    public UserTenantRepository(ISession session, IUnitOfWork unitOfWork,
        ICurrentTenant currentTenant, ICurrentUser currentUser)
        : base(session, unitOfWork, currentTenant, currentUser)
    {
        _session = session;
    }

    public async Task<TenantAccess?> GetAccessAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var user = await _session.GetAsync<User>(userId, cancellationToken);
        var account = await _session.GetAsync<Account>(tenantId, cancellationToken);
        if (user is null || account is null ||
            user.Status != UserStatus.Active || user.EmailConfirmationTime is null)
        {
            return null;
        }

        if (user.UserRole == UserRole.Admin)
        {
            return new TenantAccess(tenantId, userId, true, true, null, []);
        }

        if (account.Status != AccountStatus.Active)
        {
            return null;
        }

        var membership = await FindAsync(userId, tenantId, cancellationToken);
        if (membership is null || membership.Status != UserTenantStatus.Active)
        {
            return null;
        }

        var role = await _session.GetAsync<Role>(membership.RoleId, cancellationToken);
        if (role is null || role.TenantId != tenantId)
        {
            return null;
        }

        var permissions = await _session.Query<RolePermission>()
            .Where(permission => permission.TenantId == tenantId &&
                                 permission.RoleId == role.Id)
            .Select(permission => permission.Permission)
            .ToListAsync(cancellationToken);
        return new TenantAccess(
            tenantId, userId, false,
            string.Equals(role.Name, "Owner", StringComparison.OrdinalIgnoreCase),
            role.Id, permissions);
    }

    public async Task<UserTenant?> FindAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        return await _session.Query<UserTenant>()
            .FirstOrDefaultAsync(
                member => member.UserId == userId && member.TenantId == tenantId,
                cancellationToken);
    }

    private async Task<IReadOnlyList<UserTenant>> ListAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _session.Query<UserTenant>()
            .Where(member => member.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMember>> ListDetailsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var memberships = await ListAsync(tenantId, cancellationToken);
        if (memberships.Count == 0)
        {
            return [];
        }

        var userIds = new HashSet<Guid>();
        var roleIds = new HashSet<Guid>();
        foreach (var membership in memberships)
        {
            userIds.Add(membership.UserId);
            roleIds.Add(membership.RoleId);
        }

        var userIdValues = userIds.ToArray();
        var roleIdValues = roleIds.ToArray();
        var users = await _session.Query<User>()
            .Where(user => userIdValues.Contains(user.Id))
            .ToListAsync(cancellationToken);
        var roles = await _session.Query<Role>()
            .Where(role => roleIdValues.Contains(role.Id))
            .ToListAsync(cancellationToken);
        var userById = new Dictionary<Guid, User>(users.Count);
        foreach (var user in users)
        {
            userById.Add(user.Id, user);
        }

        var roleById = new Dictionary<Guid, Role>(roles.Count);
        foreach (var role in roles)
        {
            roleById.Add(role.Id, role);
        }
        var results = new List<TenantMember>(memberships.Count);
        foreach (var membership in memberships)
        {
            if (userById.TryGetValue(membership.UserId, out var user) &&
                roleById.TryGetValue(membership.RoleId, out var role))
            {
                results.Add(new TenantMember(
                    user.Id, user.Email, user.Name,
                    role.Id, role.Name, membership.Status));
            }
        }

        return results;
    }

    public async Task<int> CountActiveOwnersAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var owner = await _session.Query<Role>()
            .FirstOrDefaultAsync(role => role.TenantId == tenantId && role.Name == "Owner",
                cancellationToken);
        if (owner is null)
        {
            return 0;
        }

        return await _session.Query<UserTenant>().CountAsync(
            member => member.TenantId == tenantId &&
                      member.RoleId == owner.Id &&
                      member.Status == UserTenantStatus.Active,
            cancellationToken);
    }

    public async Task LockTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        await _session.CreateSQLQuery("SELECT id FROM accounts WHERE id = :tenantid FOR UPDATE")
            .SetParameter("tenantid", tenantId)
            .UniqueResultAsync(cancellationToken);
    }
}
