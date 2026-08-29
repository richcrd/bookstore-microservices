using FluentAssertions;
using Inventory.Domain.Entities;
using Inventory.Domain.Events;
using Inventory.Domain.Exceptions;

namespace Inventory.UnitTests.Domain;

public class StockItemTests
{
    [Fact]
    public void Create_ShouldInitializeStock()
    {
        var stock = StockItem.Create(Guid.NewGuid(), 10);

        stock.QuantityOnHand.Should().Be(10);
        stock.ReservedQuantity.Should().Be(0);
        stock.Available.Should().Be(10);
        stock.DomainEvents.Should().ContainSingle(e => e is StockCreated);
    }

    [Fact]
    public void Create_NegativeInitialQuantity_ShouldThrow()
    {
        Action act = () => StockItem.Create(Guid.NewGuid(), -1);

        act.Should().Throw<ArgumentException>().WithMessage("*Initial quantity*");
    }

    [Fact]
    public void AddStock_ShouldIncreaseOnHand()
    {
        var stock = StockItem.Create(Guid.NewGuid(), 10);

        stock.AddStock(5);

        stock.QuantityOnHand.Should().Be(15);
        stock.Available.Should().Be(15);
        stock.DomainEvents.Should().Contain(e => e is StockRestocked);
    }

    [Fact]
    public void AddStock_InvalidQuantity_ShouldThrow()
    {
        var stock = StockItem.Create(Guid.NewGuid(), 10);

        Action act = () => stock.AddStock(0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Reserve_ShouldDecreaseAvailable()
    {
        var stock = StockItem.Create(Guid.NewGuid(), 10);

        stock.Reserve(3);

        stock.ReservedQuantity.Should().Be(3);
        stock.Available.Should().Be(7);
        stock.DomainEvents.Should().Contain(e => e is StockReserved);
    }

    [Fact]
    public void Reserve_MoreThanAvailable_ShouldThrow()
    {
        var stock = StockItem.Create(Guid.NewGuid(), 10);

        Action act = () => stock.Reserve(11);

        act.Should().Throw<InventoryDomainException>()
            .WithMessage("*Insufficient stock*");
    }

    [Fact]
    public void Release_ShouldRestoreAvailable()
    {
        var stock = StockItem.Create(Guid.NewGuid(), 10);
        stock.Reserve(4);

        stock.Release(4);

        stock.ReservedQuantity.Should().Be(0);
        stock.Available.Should().Be(10);
        stock.DomainEvents.Should().Contain(e => e is StockReleased);
    }

    [Fact]
    public void Release_MoreThanReserved_ShouldThrow()
    {
        var stock = StockItem.Create(Guid.NewGuid(), 10);
        stock.Reserve(2);

        Action act = () => stock.Release(5);

        act.Should().Throw<InventoryDomainException>();
    }

    [Fact]
    public void DeductReserved_ShouldDecreaseBoth()
    {
        var stock = StockItem.Create(Guid.NewGuid(), 10);
        stock.Reserve(4);

        stock.DeductReserved(4);

        stock.QuantityOnHand.Should().Be(6);
        stock.ReservedQuantity.Should().Be(0);
        stock.Available.Should().Be(6);
        stock.DomainEvents.Should().Contain(e => e is StockDeducted);
    }

    [Fact]
    public void DeductReserved_MoreThanReserved_ShouldThrow()
    {
        var stock = StockItem.Create(Guid.NewGuid(), 10);
        stock.Reserve(2);

        Action act = () => stock.DeductReserved(3);

        act.Should().Throw<InventoryDomainException>();
    }

    [Fact]
    public void ClearDomainEvents_ShouldClear()
    {
        var stock = StockItem.Create(Guid.NewGuid(), 10);

        stock.ClearDomainEvents();

        stock.DomainEvents.Should().BeEmpty();
    }
}