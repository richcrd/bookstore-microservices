using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderSaga.Worker;

public class OrderSagaDbContextFactory : IDesignTimeDbContextFactory<OrderSagaDbContext>
{
    public OrderSagaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrderSagaDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=order_saga_db;Username=postgres;Password=postgres")
            .Options;

        return new OrderSagaDbContext(options);
    }
}