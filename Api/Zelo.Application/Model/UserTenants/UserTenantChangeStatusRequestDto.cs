using Zelo.Domain.UserTenants;

namespace Zelo.Application.Model.UserTenants;

public class UserTenantChangeStatusRequestDto
{
    public UserTenantStatus Status { get; set; }
}
