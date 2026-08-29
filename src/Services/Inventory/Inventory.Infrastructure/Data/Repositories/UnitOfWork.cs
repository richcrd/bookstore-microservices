using Inventory.Application.Interfaces;

namespace Inventory.Infrastructure.Data.Repositories;

public class UnitOfWork(InventoryDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);

    public void Dispose() => context.Dispose();
}