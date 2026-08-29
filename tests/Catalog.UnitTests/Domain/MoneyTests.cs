using Catalog.Domain.ValueObjects;
using FluentAssertions;

namespace Catalog.UnitTests.Domain;

public class MoneyTests
{
    [Fact]
    public void Constructor_WithNegativeAmount_ThrowsArgumentException()
    {
        var act = () => new Money(-1m, "USD");

        act.Should().Throw<ArgumentException>().WithMessage("*negative*");
    }

    [Fact]
    public void Constructor_WithEmptyCurrency_ThrowsArgumentException()
    {
        var act = () => new Money(10m, "");

        act.Should().Throw<ArgumentException>().WithMessage("*currency*");
    }
    
    [Fact]
    public void Constructor_WithLowercaseCurrency_UppercasesIt()
    {
        var money = new Money(10m, "usd");

        money.Currency.Should().Be("USD");
    }
    
    [Fact]
    public void Add_WithSameCurrency_ReturnsSum()
    {
        var result = new Money(10m, "USD").Add(new Money(15m, "USD"));

        result.Amount.Should().Be(25m);
        result.Currency.Should().Be("USD");
    }
    
    [Fact]
    public void Add_WithDifferentCurrency_ThrowsInvalidOperationException()
    {
        var act = () => new Money(10m, "USD").Add(new Money(10m, "EUR"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*different currencies*");
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_AreEqual()
    {
        var money1 = new Money(10m, "USD");
        var money2 = new Money(10m, "USD");

        money1.Should().Be(money2);
    }
}