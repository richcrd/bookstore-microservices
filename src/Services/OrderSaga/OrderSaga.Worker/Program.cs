using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace OrderSaga.Worker;

public class Program
{
    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;

                var otlpEndpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

                services.AddOpenTelemetry()
                    .ConfigureResource(r => r.AddService("OrderSaga.Worker"))
                    .WithTracing(t => t
                        .AddEntityFrameworkCoreInstrumentation()
                        .AddSource("MassTransit", "OrderSaga.Worker")
                        .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
                    .WithMetrics(m => m
                        .AddMeter("MassTransit")
                        .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));

                services.AddDbContext<OrderSagaDbContext>(options =>
                    options.UseNpgsql(configuration.GetConnectionString("OrderSagaDb")));

                services.AddMassTransit(x =>
                {
                    x.AddSagaStateMachine<OrderSagaStateMachine, OrderSagaState>()
                        .EntityFrameworkRepository(r =>
                        {
                            r.ExistingDbContext<OrderSagaDbContext>();
                            r.UsePostgres();
                        });

                    x.AddConsumer<PaymentGatewayConsumer>();

                    x.AddEntityFrameworkOutbox<OrderSagaDbContext>(o =>
                    {
                        o.UsePostgres();
                    });

                    x.SetKebabCaseEndpointNameFormatter();

                    x.AddConfigureEndpointsCallback((context, name, cfg) =>
                    {
                        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
                        cfg.UseEntityFrameworkOutbox<OrderSagaDbContext>(context);
                    });

                    x.AddOpenTelemetry();

                    x.UsingRabbitMq((context, cfg) =>
                    {
                        cfg.Host(configuration["RabbitMQ:Host"] ?? "rabbitmq://localhost");
                        cfg.ConfigureEndpoints(context);
                        cfg.UseCircuitBreaker(cb =>
                        {
                            cb.TrackingPeriod = TimeSpan.FromSeconds(60);
                            cb.TripThreshold = 5;
                            cb.ActiveThreshold = 10;
                            cb.ResetInterval = TimeSpan.FromSeconds(30);
                        });
                    });
                });
            });
}