using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Interfaces;
using Orders.Infrastructure.Data;
using Orders.Infrastructure.Data.Repositories;
using Orders.Infrastructure.External;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<OrdersDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>()
                .GetConnectionString("OrdersDb");
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHttpClient<ICatalogService, CatalogService>((sp, client) =>
        {
            client.BaseAddress = new Uri(sp.GetRequiredService<IConfiguration>()["CatalogApi:BaseAddress"]!);
            client.Timeout = TimeSpan.FromSeconds(5);
        })
        .AddResilienceHandler("catalog-pipeline", builder =>
        {
            // 3 retries with exponential backoff
            builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>()
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(400),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(r => !r.IsSuccessStatusCode || r.StatusCode == HttpStatusCode.TooManyRequests)
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutRejectedException>()
            });

            builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>()
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(15)
            });

            builder.AddTimeout(new TimeoutStrategyOptions()
            {
                Timeout = TimeSpan.FromSeconds(3)
            });
        });

        return services;
    }
}