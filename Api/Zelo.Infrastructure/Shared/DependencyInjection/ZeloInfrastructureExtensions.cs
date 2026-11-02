using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using Zelo.Application.Shared.Context;
using Zelo.Application.Shared.Data;
using Zelo.Application.Shared.Events;
using Zelo.Domain.Shared.Repositories;
using Zelo.Infrastructure.Shared.Data;
using Zelo.Infrastructure.Shared.Data.Repositories;
using Zelo.Infrastructure.Shared.Events;

namespace Zelo.Infrastructure.Shared.DependencyInjection;

public static class ZeloInfrastructureExtensions
{
    public static ZeloInfrastructureBuilder AddZeloInfrastructure(this IServiceCollection services, Func<IServiceProvider, ISessionFactory> sessionFactory)
    {
        services.AddSingleton<ISessionFactory>(sessionFactory);
        services.AddScoped(provider =>
        {
            var interceptor = new AuditInterceptor(provider.GetRequiredService<ICurrentUser>());
            var session = provider.GetRequiredService<ISessionFactory>().WithOptions().Interceptor(interceptor).OpenSession();
            if (provider.GetRequiredService<ICurrentTenant>().Id is Guid tenantId)
            {
                session.EnableFilter("tenant").SetParameter("tenantId", tenantId);
            }

            return session;
        });
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        services.AddScoped<IUnitOfWork, NHibernateUnitOfWork>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        return new ZeloInfrastructureBuilder(services);
    }
}
