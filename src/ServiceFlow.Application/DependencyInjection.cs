using Microsoft.Extensions.DependencyInjection;

namespace ServiceFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
