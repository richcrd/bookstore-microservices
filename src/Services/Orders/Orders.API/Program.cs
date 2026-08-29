using MassTransit;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Orders.API.Consumers;
using SharedKernel.Security;
using Orders.API.Middleware;
using Orders.Application;
using Orders.Application.Commands.Validation;
using Orders.Infrastructure;
using Orders.Infrastructure.Data;
using SharedKernel.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure();
builder.Services.AddApplication();
builder.Services.AddValidatorsFromAssembly(typeof(CreateOrderRequestValidator).Assembly);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddHealthChecks()
    .AddNpgSql(sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("OrdersDb")!);

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationBuilder();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ChangeOrderStatusCommandConsumer>();

    x.AddEntityFrameworkOutbox<OrdersDbContext>(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(1);
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.SetKebabCaseEndpointNameFormatter();

    x.AddConfigureEndpointsCallback((context, name, cfg) =>
    {
        cfg.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5)));
        cfg.UseEntityFrameworkOutbox<OrdersDbContext>(context);
    });

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

builder.Services.AddServiceTelemetry(builder.Configuration, "Orders.API");

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.Migrate();
}

app.Run();

public partial class Program { }

