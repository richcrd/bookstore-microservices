using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Commands;
using Orders.Application.Queries;

namespace Orders.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateOrderCommand>();
        services.AddScoped<GetOrderByIdQuery>();
        services.AddScoped<GetOrdersQuery>();
        services.AddScoped<UpdateOrderStatusCommand>();

        return services;
    }
}