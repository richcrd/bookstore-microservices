using FluentValidation;
using Inventory.API.Middleware;
using Inventory.Application;
using Inventory.Application.Commands.Validation;
using Inventory.Infrastructure;

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
