var builder = DistributedApplication.CreateBuilder(args);

const string postgres = "Host=localhost;Port=5432;Database={0};Username=postgres;Password=postgres";
const string rabbit = "rabbitmq://localhost:5672";

var apiGateway = builder.AddProject<Projects.ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5080, name: "http");

var auth = builder.AddProject<Projects.Auth_API>("auth")
    .WithHttpEndpoint(port: 5100, name: "http");

var catalog = builder.AddProject<Projects.Catalog_API>("catalog")
    .WithHttpEndpoint(port: 5038, name: "http")
    .WithEnvironment("ConnectionStrings__CatalogDb", string.Format(postgres, "catalog_db"));

var orders = builder.AddProject<Projects.Orders_API>("orders")
    .WithHttpEndpoint(port: 5248, name: "http")
    .WithEnvironment("ConnectionStrings__OrdersDb", string.Format(postgres, "orders_db"))
    .WithEnvironment("RabbitMQ__Host", rabbit);

var inventory = builder.AddProject<Projects.Inventory_API>("inventory")
    .WithHttpEndpoint(port: 5208, name: "http")
    .WithEnvironment("ConnectionStrings__InventoryDb", string.Format(postgres, "inventory_db"))
    .WithEnvironment("RabbitMQ__Host", rabbit);

builder.AddProject<Projects.OrderSaga_Worker>("saga")
    .WithEnvironment("ConnectionStrings__OrderSagaDb", string.Format(postgres, "order_saga_db"))
    .WithEnvironment("RabbitMQ__Host", rabbit);

apiGateway.WaitFor(auth).WaitFor(catalog).WaitFor(orders).WaitFor(inventory);

builder.Build().Run();