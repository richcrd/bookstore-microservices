using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Data.Repositories;

public class StockItemRepository(InventoryDbContext context) : IStockItemRepository
{
    public async Task<StockItem?> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default)
        => await context.StockItems.SingleOrDefaultAsync(s => s.BookId == bookId, cancellationToken);

    public async Task<StockItem?> GetByIdAsync(StockId id, CancellationToken cancellationToken = default)
        => await context.StockItems.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
    
    public async Task<(IReadOnlyList<StockItem> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = context.StockItems.AsNoTracking().AsQueryable();

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task AddAsync(StockItem stockItem, CancellationToken cancellationToken = default)
        => await context.StockItems.AddAsync(stockItem, cancellationToken);
}