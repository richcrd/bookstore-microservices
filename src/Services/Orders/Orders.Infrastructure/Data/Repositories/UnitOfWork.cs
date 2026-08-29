using Orders.Application.Interfaces;

namespace Orders.Infrastructure.Data.Repositories;

public class UnitOfWork(OrdersDbContext context) : IUnitOfWork
{
    public void Dispose() => context.Dispose();

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
    
}