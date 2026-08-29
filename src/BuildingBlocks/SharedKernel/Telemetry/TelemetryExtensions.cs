using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SharedKernel.Telemetry;

public static class TelemetryExtensions
{
    public static IServiceCollection AddServiceTelemetry(this IServiceCollection services,
        IConfiguration configuration, string serviceName)
    {
        var endpoint = configuration["OpenTelemetry:Endpoint"] ?? "http://localhost:4317";

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddSource("MassTransit", serviceName)
                .AddOtlpExporter(o => o.Endpoint = new Uri(endpoint)))
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http", "MassTransit", serviceName)
                .AddPrometheusExporter());

        return services;
    }

    public static IApplicationBuilder UseServiceTelemetry(this IApplicationBuilder app)
        => app.UseOpenTelemetryPrometheusScrapingEndpoint();
}