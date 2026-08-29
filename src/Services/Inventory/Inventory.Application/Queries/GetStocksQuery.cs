using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;

namespace Inventory.Application.Queries;

public record PaginatedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public class GetStocksQuery(IStockItemRepository stockItemRepository)
{
    public async Task<PaginatedResult<StockItemDto>> ExecuteAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await stockItemRepository.GetAllAsync(page, pageSize, cancellationToken);

        var dtoItems = items.Select(stockItem => new StockItemDto(
            stockItem.Id.Value,
            stockItem.BookId,
            stockItem.QuantityOnHand,
            stockItem.ReservedQuantity,
            stockItem.Available,
            stockItem.CreatedAt,
            stockItem.UpdatedAt)).ToList();

        return new PaginatedResult<StockItemDto>(dtoItems, totalCount, page, pageSize);
    }
}