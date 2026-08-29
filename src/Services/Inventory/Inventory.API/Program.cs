using FluentValidation;
using Inventory.API.Consumers;
using Inventory.API.Middleware;
using Inventory.Application;
using Inventory.Application.Commands.Validation;
using Inventory.Infrastructure;
using Inventory.Infrastructure.Data;
using MassTransit;
using SharedKernel.Security;
using SharedKernel.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure();
builder.Services.AddApplication();
builder.Services.AddValidatorsFromAssembly(typeof(CreateStockItemRequestValidator).Assembly);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("InventoryDb")!);

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationBuilder();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();
    x.AddConsumer<OrderStatusChangedConsumer>();

    x.AddEntityFrameworkOutbox<InventoryDbContext>(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(1);
        o.UsePostgres();
    });
    
    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        cfg.UseEntityFrameworkOutbox<InventoryDbContext>(context);
    });

    x.SetKebabCaseEndpointNameFormatter();

    x.AddOpenTelemetry();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "rabbitmq://localhost");
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

builder.Services.AddServiceTelemetry(builder.Configuration, "Inventory.API");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseServiceTelemetry();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
