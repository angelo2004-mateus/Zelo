namespace Zelo.Domain.Roles;

public interface IRoleRepository
{
    Task<Role?> FindAsync(Guid tenantId, Guid roleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Role>> ListAsync(Guid tenantId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListPermissionsAsync(
        Guid tenantId, Guid roleId, CancellationToken cancellationToken);
}
