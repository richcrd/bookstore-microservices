namespace Catalog.Domain.ValueObjects;

public record Isbn
{
    public string Value { get; }

    public Isbn(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("ISBN is required.", nameof(value));
        }

        var cleaned = value.Replace("-", "").Replace(" ", "");

        if (cleaned.Length is not (10 or 13))
        {
            throw new ArgumentException($"ISBN must be 10 or 13 digits. Got {cleaned.Length}.", nameof(value));
        }

        Value = cleaned;
    }

    public override string ToString() => Value;

    public static implicit operator string(Isbn isbn) => isbn.Value;
}
