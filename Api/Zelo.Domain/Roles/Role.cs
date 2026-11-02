using Zelo.Domain.Shared.Entities;

namespace Zelo.Domain.Roles;

public class Role : IEntity, IMultiTenant
{
    public virtual Guid Id { get; set; }
    public virtual Guid TenantId { get; set; }
    public virtual string Name { get; set; }

    public Role() { }
}
