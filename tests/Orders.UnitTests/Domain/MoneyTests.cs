using FluentAssertions;
using Orders.Domain.ValueObjects;

namespace Orders.UnitTests.Domain;

public class MoneyTests
{
    [Fact]
    public void Constructor_ShouldNormalizeCurrencyToUpper()
    {
        new Money(10m, "usd").Currency.Should().Be("USD");
    }

    [Fact]
    public void Constructor_NegativeAmount_ShouldThrow()
    {
        Action act = () => new Money(-1m, "USD");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Add_SameCurrency_ShouldSum()
    {
        var a = new Money(10m, "USD");
        var b = new Money(5.5m, "USD");

        a.Add(b).Amount.Should().Be(15.5m);
    }

    [Fact]
    public void Add_DifferentCurrencies_ShouldThrow()
    {
        var a = new Money(10m, "USD");
        var b = new Money(10m, "EUR");

        Action act = () => a.Add(b);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void WithAmount_ShouldKeepCurrency()
    {
        new Money(10m, "USD").WithAmount(20m).Currency.Should().Be("USD");
    }

    [Fact]
    public void Zero_ShouldReturnZero()
    {
        Money.Zero().Amount.Should().Be(0m);
        Money.Zero("EUR").Currency.Should().Be("EUR");
    }
}