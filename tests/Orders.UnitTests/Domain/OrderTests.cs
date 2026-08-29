using FluentAssertions;
using Orders.Domain.Entities;
using Orders.Domain.Enums;
using Orders.Domain.Events;
using Orders.Domain.ValueObjects;

namespace Orders.UnitTests.Domain;

public class OrderTests
{
    [Fact]
    public void Create_ShouldInitializeOrdersAsPending()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid().ToString("D"));

        order.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().BeEmpty();
        order.Total.Should().Be(Money.Zero());
        order.DomainEvents.Should().ContainSingle(e => e is OrderCreated);
    }

    [Fact]
    public void Create_WithEmotyCustomerId_ShouldThrow()
    {
        Action act = () => Order.Create(Guid.Empty, Guid.NewGuid().ToString("D"));

        act.Should().Throw<ArgumentException>().WithMessage("*CustomerId*");
    }

    [Fact]
    public void AddItem_ShouldComputeTotalAcrossItems()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid().ToString("D"));

        order.AddItem(Guid.NewGuid(), "Book A", new Money(10m, "USD"), 2);
        order.AddItem(Guid.NewGuid(), "Book B", new Money(5m, "USD"), 3);

        order.Items.Should().HaveCount(2);
        order.Total.Amount.Should().Be(35m);
        order.Total.Currency.Should().Be("USD");
    }
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_WithInvalidQuantity_ShouldThrow(int quantity)
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid().ToString("D"));

        Action act = () => order.AddItem(Guid.NewGuid(), "Book", new Money(10m, "USD"), quantity);

        act.Should().Throw<ArgumentException>().WithMessage("*Quantity*");
    }
    
    [Fact]
    public void ChangeStatus_FollowingValidFlow_ShouldSucceed()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid().ToString("D"));

        order.ChangeStatus(OrderStatus.Paid);
        order.ChangeStatus(OrderStatus.Shipped);
        order.ChangeStatus(OrderStatus.Delivered);

        order.Status.Should().Be(OrderStatus.Delivered);
    }
    
    [Fact]
    public void ChangeStatus_InvalidTransition_ShouldThrow()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid().ToString("D"));

        Action act = () => order.ChangeStatus(OrderStatus.Delivered);

        act.Should().Throw<InvalidOperationException>();
    }
    
    [Fact]
    public void ChangeStatus_ShouldAddStatusChangedEvent()
    {
        var order = Order.Create(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        order.ClearDomainEvents();

        order.ChangeStatus(OrderStatus.Paid);

        order.DomainEvents.Should().ContainSingle(e => e is OrderStatusChanged);
    }

    [Fact]
    public void Cancelled_ShouldBeAllowed_OnlyFromPendingOrPaid()
    {
        var pending = Order.Create(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        pending.ChangeStatus(OrderStatus.Cancelled);
        pending.Status.Should().Be(OrderStatus.Cancelled);

        var paid = Order.Create(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        paid.ChangeStatus(OrderStatus.Paid);
        paid.ChangeStatus(OrderStatus.Cancelled);
        paid.Status.Should().Be(OrderStatus.Cancelled);

        var shipped = Order.Create(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        shipped.ChangeStatus(OrderStatus.Paid);
        shipped.ChangeStatus(OrderStatus.Shipped);

        Action act = () => shipped.ChangeStatus(OrderStatus.Cancelled);

        act.Should().Throw<InvalidOperationException>();
    }
}