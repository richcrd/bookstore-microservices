using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SharedKernel.Telemetry;

public static class TelemetryExtensions
{
    public static IServiceCollection AddServiceTelemetry(this IServiceCollection services,
        IConfiguration configuration, string serviceName)
    {
        var endpoint = ResolveOtlpEndpoint(configuration);

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddSource("MassTransit", serviceName)
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(OtlpSignalEndpoint(endpoint, "traces"));
                    o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                }))
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel",
                    "System.Net.Http", "MassTransit", serviceName)
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(OtlpSignalEndpoint(endpoint, "metrics"));
                    o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                })
                .AddPrometheusExporter())
            .WithLogging(l => l
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(OtlpSignalEndpoint(endpoint, "logs"));
                    o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                }));

        return services;
    }

    public static IApplicationBuilder UseServiceTelemetry(this IApplicationBuilder app)
        => app.UseOpenTelemetryPrometheusScrapingEndpoint();

    private static string ResolveOtlpEndpoint(IConfiguration configuration) =>
        Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
        ?? configuration["OpenTelemetry:Endpoint"]
        ?? "http://localhost:4318";

    private static string OtlpSignalEndpoint(string baseEndpoint, string signal) =>
        $"{baseEndpoint.TrimEnd('/')}/v1/{signal}";
}