using Microsoft.Extensions.DependencyInjection;

namespace Zelo.Infrastructure.Shared.DependencyInjection;

public sealed class ZeloInfrastructureBuilder
{
    public IServiceCollection Services { get; }

    public ZeloInfrastructureBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public ZeloInfrastructureBuilder AddModules<TModule>() where TModule : IZeloModule, new()
    {
        new TModule().AddServices(Services);
        return this;
    }
}
