using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMediator(config =>
        {
            config.ServiceLifetime = ServiceLifetime.Scoped;
        });

        return services;
    }
}
