using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using SharedKernel.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ =>
            new FixedWindowRateLimiterOptions()
            {
                PermitLimit = 20,
                Window = TimeSpan.FromSeconds(15),
                QueueLimit = 0
            });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = "15";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Too many requests. Please retry later."
        }, cancellationToken);
    };
});

builder.Services.AddHealthChecks();

builder.Services.AddServiceTelemetry(builder.Configuration, "ApiGateway");

var app = builder.Build();

app.UseServiceTelemetry();
app.UseRateLimiter();

app.MapReverseProxy();
app.MapHealthChecks("/health");

app.Run();
