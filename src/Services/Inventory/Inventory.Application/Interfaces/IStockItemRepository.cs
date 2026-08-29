using Inventory.Domain.Entities;
using Inventory.Domain.ValueObjects;

namespace Inventory.Application.Interfaces;

public interface IStockItemRepository
{
    Task<StockItem?> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);
    Task<StockItem?> GetByIdAsync(StockId id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<StockItem> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task AddAsync(StockItem stockItem, CancellationToken cancellationToken = default);
}