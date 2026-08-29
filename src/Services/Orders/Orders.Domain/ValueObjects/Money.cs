namespace Orders.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = "USD") => new Money(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new InvalidOperationException(
                $"Cannot add money with different currencies: {Currency} and {other.Currency}");
        }

        return new Money(Amount + other.Amount, Currency);
    }

    public Money WithAmount(decimal newAmount) => new Money(newAmount, Currency);
}