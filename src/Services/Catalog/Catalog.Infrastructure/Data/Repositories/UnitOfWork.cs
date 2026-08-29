using Catalog.Application.Interfaces;

namespace Catalog.Infrastructure.Data.Repositories;

public class UnitOfWork(CatalogDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);

    public void Dispose() => context.Dispose();
}