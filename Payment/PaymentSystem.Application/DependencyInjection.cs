using Microsoft.Extensions.DependencyInjection;
using PaymentSystem.Application.Queries.PaymentQueries;

namespace PaymentSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetPaymentByIdQuery).Assembly));

        return services;

    }
}
