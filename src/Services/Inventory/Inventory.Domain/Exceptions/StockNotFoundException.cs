namespace Inventory.Domain.Exceptions;

public class StockNotFoundException(string message) : InventoryDomainException(message);