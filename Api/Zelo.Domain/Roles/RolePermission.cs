using Zelo.Domain.Shared.Entities;

namespace Zelo.Domain.Roles;

public class RolePermission : Entity, IMultiTenant
{
    public virtual Guid TenantId { get; set; }
    public virtual Guid RoleId { get; set; }
    public virtual string Permission { get; set; }

    public RolePermission() { }
}
