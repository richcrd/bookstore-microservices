using Inventory.Domain.Events;
using Inventory.Domain.Exceptions;
using Inventory.Domain.ValueObjects;

namespace Inventory.Domain.Entities;

public class StockItem
{
    private readonly List<IDomainEvent> _domainEvents = [];
    
    public StockId Id { get; private set; }
    public Guid BookId { get; private set; }
    public int QuantityOnHand { get; private set; }
    public int ReservedQuantity { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public int Available => QuantityOnHand - ReservedQuantity;

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    private StockItem() { }

    public static StockItem Create(Guid bookId, int initialQuantity)
    {
        if (bookId == Guid.Empty)
        {
            throw new ArgumentException("BookId is required.", nameof(bookId));
        }
        
        if (initialQuantity < 0)
        {
            throw new ArgumentException("Initial quantity cannot be negative.", nameof(initialQuantity));
        }

        var stockItem = new StockItem()
        {
            Id = StockId.New(),
            BookId = bookId,
            QuantityOnHand = initialQuantity,
            ReservedQuantity = 0,
            CreatedAt = DateTime.UtcNow
        };
        
        stockItem._domainEvents.Add(new StockCreated(stockItem.Id.Value, stockItem.BookId, initialQuantity));

        return stockItem;
    }
    
    public void AddStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        QuantityOnHand += quantity;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(new StockRestocked(Id.Value, BookId, quantity));
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        if (Available < quantity)
        {
            throw new InventoryDomainException(
                $"Insufficient stock. Available: {Available}, requested: {quantity}.");
        }

        ReservedQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(new StockReserved(Id.Value, BookId, quantity));
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        if (ReservedQuantity < quantity)
        {
            throw new InventoryDomainException(
                $"Cannot release more than reserved. Reserved: {ReservedQuantity}, requested: {quantity}.");
        }

        ReservedQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(new StockReleased(Id.Value, BookId, quantity));
    }

    public void DeductReserved(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        }

        if (ReservedQuantity < quantity)
        {
            throw new InventoryDomainException(
                $"Cannot deduct more than reserved. Reserved: {ReservedQuantity}, requested: {quantity}.");
        }

        QuantityOnHand -= quantity;
        ReservedQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;

        _domainEvents.Add(new StockDeducted(Id.Value, BookId, quantity));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}