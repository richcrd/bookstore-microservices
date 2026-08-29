namespace Inventory.Application.DTOs;

public record CreateStockItemRequest(Guid BookId, int InitialQuantity);