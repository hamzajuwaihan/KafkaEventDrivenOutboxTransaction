using Microsoft.Extensions.DependencyInjection;
using OrdersSystem.Application.Queries.OrderQueries;

namespace OrdersSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetOrderByIdByIdQuery).Assembly));
        
        return services;
    }
}
