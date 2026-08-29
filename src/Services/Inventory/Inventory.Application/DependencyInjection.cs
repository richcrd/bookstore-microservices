using Inventory.Application.Commands;
using Inventory.Application.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateStockCommand>();
        services.AddScoped<AddStockCommand>();
        services.AddScoped<ReserveStockCommand>();
        services.AddScoped<ReleaseStockCommand>();
        services.AddScoped<DeductStockCommand>();

        services.AddScoped<GetStockByBookQuery>();
        services.AddScoped<GetStockByIdQuery>();
        services.AddScoped<GetStocksQuery>();

        return services;
    }
}