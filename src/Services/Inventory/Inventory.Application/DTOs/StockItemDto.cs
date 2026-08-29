namespace Inventory.Application.DTOs;

public record StockItemDto(
    Guid Id,
    Guid BookId,
    int QuantityOnHand,
    int ReservedQuantity,
    int Available,
    DateTime CreatedAt,
    DateTime? UpdatedAt);