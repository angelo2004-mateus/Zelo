using NHibernate;
using NHibernate.Linq;
using Zelo.Domain.Roles;

namespace Zelo.Infrastructure.Data.Repositories.Roles;

public class RoleRepository : IRoleRepository
{
    private readonly ISession _session;

    public RoleRepository(ISession session)
    {
        _session = session;
    }

    public async Task<Role?> FindAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken)
    {
        return await _session.Query<Role>().FirstOrDefaultAsync(
            role => role.TenantId == tenantId && role.Id == roleId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> ListAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _session.Query<Role>()
            .Where(role => role.TenantId == tenantId)
            .OrderBy(role => role.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListPermissionsAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken)
    {
        return await _session.Query<RolePermission>()
            .Where(permission => permission.TenantId == tenantId &&
                                 permission.RoleId == roleId)
            .Select(permission => permission.Permission)
            .ToListAsync(cancellationToken);
    }

}
