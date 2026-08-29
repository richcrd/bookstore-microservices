using Orders.Domain.ValueObjects;

namespace Orders.Domain.Entities;

public class OrderItem
{
    public OrderId OrderId { get; private set; }
    public Guid BookId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Money UnitPrice { get; private set; } = null!;
    public int Quantity { get; private set; }

    public Money LineTotal => UnitPrice.WithAmount(UnitPrice.Amount * Quantity);
    
    private OrderItem() { }

    internal OrderItem(OrderId orderId, Guid bookId, string title, Money unitPrice, int quantity)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Item title is required.", nameof(title));
        }

        OrderId = orderId;
        BookId = bookId;
        Title = title;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }
}
