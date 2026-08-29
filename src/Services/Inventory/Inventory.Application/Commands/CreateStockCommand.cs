using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;

namespace Inventory.Application.Commands;

public class CreateStockCommand(IStockItemRepository stockItemRepository, IUnitOfWork unitOfWork)
{
    public async Task<StockItemDto> ExecuteAsync(CreateStockItemRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await stockItemRepository.GetByBookIdAsync(request.BookId, cancellationToken);
        if (existing is not null)
        {
            throw new InventoryDomainException($"Stock already exists for book {request.BookId}.");
        }

        var stockItem = StockItem.Create(request.BookId, request.InitialQuantity);

        await stockItemRepository.AddAsync(stockItem, cancellationToken);
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