using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Driver;
using NHibernate.Engine;
using NHibernate.Mapping.ByCode;
using Zelo.Infrastructure.Data.Mappings.Accounts;
using Zelo.Infrastructure.Data.Mappings.Companies;
using Zelo.Infrastructure.Data.Mappings.Invitations;
using Zelo.Infrastructure.Data.Mappings.Roles;
using Zelo.Infrastructure.Data.Mappings.Users;
using Zelo.Infrastructure.Data.Mappings.UserTenants;

namespace Zelo.Infrastructure.Data;

public static class NHibernateSessionFactory
{
    public static ISessionFactory Create(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var mapper = new ModelMapper();
        mapper.AddMapping<UserMap>();
        mapper.AddMapping<UserPasswordCredentialMap>();
        mapper.AddMapping<UserAuthTokenMap>();
        mapper.AddMapping<UserAuthSessionMap>();
        mapper.AddMapping<UserTotpCredentialMap>();
        mapper.AddMapping<AccountMap>();
        mapper.AddMapping<RoleMap>();
        mapper.AddMapping<RolePermissionMap>();
        mapper.AddMapping<UserTenantMap>();
        mapper.AddMapping<CompanyMap>();
        mapper.AddMapping<InvitationMap>();

        var configuration = new Configuration();
        configuration.AddFilterDefinition(new FilterDefinition(
            "tenant", "tenantid = :tenantId",
            new Dictionary<string, NHibernate.Type.IType>
            {
                ["tenantId"] = NHibernateUtil.Guid
            }, false));
        configuration.DataBaseIntegration(database =>
        {
            database.ConnectionString = connectionString;
            database.Dialect<PostgreSQL83Dialect>();
            database.Driver<NpgsqlDriver>();
        });
        configuration.AddMapping(mapper.CompileMappingForAllExplicitlyAddedEntities());

        return configuration.BuildSessionFactory();
    }
}
