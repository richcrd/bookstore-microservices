using Catalog.Application.Commands;
using Catalog.Application.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<CreateBookCommand>();
        services.AddScoped<UpdateBookPriceCommand>();
        services.AddScoped<CreateCategoryCommand>();
        services.AddScoped<GetBookByIdQuery>();
        services.AddScoped<GetBooksQuery>();
        services.AddScoped<GetCategoryByIdQuery>();
        
        return services;
    }
}
