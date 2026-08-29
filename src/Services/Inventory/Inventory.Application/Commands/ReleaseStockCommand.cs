using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;

namespace Inventory.Application.Commands;

public class ReleaseStockCommand(IStockItemRepository stockItemRepository, IUnitOfWork unitOfWork)
{
    public async Task<StockItemDto> ExecuteAsync(Guid bookId, StockOperationRequest request, CancellationToken cancellationToken = default)
    {
        var stockItem = await stockItemRepository.GetByBookIdAsync(bookId, cancellationToken)
                        ?? throw new StockNotFoundException($"No stock found for book {bookId}.");

        stockItem.Release(request.Quantity);
        await unitOfWork.SaveChangesAsync(cancellationToken);

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