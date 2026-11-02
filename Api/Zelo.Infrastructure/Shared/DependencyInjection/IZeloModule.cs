using Microsoft.Extensions.DependencyInjection;

namespace Zelo.Infrastructure.Shared.DependencyInjection;

public interface IZeloModule
{
    void AddServices(IServiceCollection services);
}
