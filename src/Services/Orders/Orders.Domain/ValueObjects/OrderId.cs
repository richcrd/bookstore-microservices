namespace Orders.Domain.ValueObjects;

/// <summary>
/// Represents the unique identifier of an order
/// </summary>
public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new OrderId(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}