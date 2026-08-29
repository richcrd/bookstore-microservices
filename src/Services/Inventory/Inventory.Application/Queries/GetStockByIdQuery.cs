using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Inventory.Domain.ValueObjects;

namespace Inventory.Application.Queries;

public class GetStockByIdQuery(IStockItemRepository stockItemRepository)
{
    public async Task<StockItemDto> ExecuteAsync(StockId id, CancellationToken cancellationToken = default)
    {
        var stockItem = await stockItemRepository.GetByIdAsync(id, cancellationToken)
                        ?? throw new StockNotFoundException($"No stock found with id {id}.");

        return MapToDto(stockItem);
    }

    private static StockItemDto MapToDto(StockItem stockItem) => new(
        stockItem.Id.Value,
        stockItem.BookId,
        stockItem.QuantityOnHand,
        stockItem.ReservedQuantity,
        stockItem.Available,
        stockItem.CreatedAt,
        stockItem.UpdatedAt);
}