namespace Zelo.Application.Model.Roles;

public class RoleDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public required IReadOnlyList<string> Permissions { get; set; }
}
