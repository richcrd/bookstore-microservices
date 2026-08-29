namespace Inventory.Domain.ValueObjects;

public readonly record struct StockId(Guid Value)
{
    public static StockId New() => new StockId(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}