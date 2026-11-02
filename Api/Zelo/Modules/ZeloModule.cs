using Microsoft.Extensions.DependencyInjection;
using Zelo.Application.Accounts.Handlers;
using Zelo.Application.Accounts.Services;
using Zelo.Application.Contract;
using Zelo.Application.Services;
using Zelo.Application.Shared.Auths;
using Zelo.Application.Shared.Authorization;
using Zelo.Application.Shared.Context;
using Zelo.Application.Shared.Events;
using Zelo.Application.Users.Services;
using Zelo.Domain.Accounts;
using Zelo.Domain.Accounts.Events;
using Zelo.Domain.Invitations;
using Zelo.Domain.Roles;
using Zelo.Domain.UserTenants;
using Zelo.Domain.Users;
using Zelo.Infrastructure.Data.Repositories.Accounts;
using Zelo.Infrastructure.Data.Repositories.Invitations;
using Zelo.Infrastructure.Data.Repositories.Roles;
using Zelo.Infrastructure.Data.Repositories.UserTenants;
using Zelo.Infrastructure.Data.Repositories.Users;
using Zelo.Infrastructure.Data.Stores.Users;
using Zelo.Infrastructure.Shared.DependencyInjection;
using Zelo.Web.Api.Shared.Context;

namespace Zelo.Web.Api.Modules;

public sealed class ZeloModule : IZeloModule
{
    public void AddServices(IServiceCollection services)
    {
        #region Request Context

        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<ICurrentTenant, HttpCurrentTenant>();
        services.AddScoped<ICurrentCompany, HttpCurrentCompany>();

        #endregion

        #region Repositories and Stores

        services.AddScoped<IUserAuthStore<User>, UserAuthStore>();
        services.AddScoped<IAuthSessionStore, AuthSessionStore>();
        services.AddScoped<IAuthLifecycleStore, AuthLifecycleStore>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUserTenantRepository, UserTenantRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        #endregion

        #region Application Services

        services.AddScoped<IAuthFlowAppService, AuthFlowAppService>();
        services.AddScoped<AccountProvisioningService>();
        services.AddScoped<AccountRoleService>();
        services.AddScoped<AccountRoleManagementService>();
        services.AddScoped<AccountSetupService>();
        services.AddScoped<UserRegistrationService>();
        services.AddScoped<ITenantAuthorizationService, TenantAuthorizationService>();
        services.AddScoped<IAccountAppService, AccountAppService>();
        services.AddScoped<ICompanyAppService, CompanyAppService>();
        services.AddScoped<IUserAppService, UserAppService>();
        services.AddScoped<IRoleAppService, RoleAppService>();
        services.AddScoped<IUserTenantAppService, UserTenantAppService>();
        services.AddScoped<IInvitationAppService, InvitationAppService>();

        #endregion

        #region Domain Event Handlers

        services.AddScoped<IDomainEventHandler<AccountCreated>, AccountCreatedHandler>();

        #endregion
    }
}
