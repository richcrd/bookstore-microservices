using FluentAssertions;
using NSubstitute;
using Orders.Application;
using Orders.Application.DTOs;
using Orders.Application.Interfaces;
using Orders.Domain.Entities;
using Orders.Domain.ValueObjects;

namespace Orders.UnitTests.Application;

public class CreateOrderCommandTests
{
    [Fact]
    public async Task Execute_ShouldCreateOrder_WithSnapshotFromCatalog()
    {
        var bookId = Guid.NewGuid();
        var catalog = Substitute.For<ICatalogService>();
        catalog.GetBookAsync(bookId, Arg.Any<CancellationToken>())
            .Returns(new BookSnapshot(bookId, "Clean Architecture V2", 199.99m, "USD"));

        var repository = Substitute.For<IOrderRepository>();
        Order? added = null;
        repository.AddAsync(Arg.Do<Order>(o => added = o), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var unitOfWork = Substitute.For<IUnitOfWork>();

        var command = new CreateOrderCommand(repository, unitOfWork, catalog);

        var result = await command.ExecuteAsync(new CreateOrderRequest(
            Guid.NewGuid(),
            [new CreateOrderItemRequest(bookId, 2)]),
            Guid.NewGuid().ToString("D"));

        result.Created.Should().BeTrue();
        result.Order.Status.Should().Be("Pending");
        result.Order.Total.Should().Be(399.98m);
        result.Order.Items.Should().HaveCount(1);
        result.Order.Items[0].Title.Should().Be("Clean Architecture V2");

        added.Should().NotBeNull();
        added!.Items.Should().HaveCount(1);
        added.Items[0].UnitPrice.Amount.Should().Be(199.99m);

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_SameIdempotencyKey_ShouldReturnExistingOrderWithoutAdding()
    {
        var bookId = Guid.NewGuid();
        var key = Guid.NewGuid().ToString("D");
        var request = new CreateOrderRequest(
            Guid.NewGuid(),
            [new CreateOrderItemRequest(bookId, 1)]);

        var catalog = Substitute.For<ICatalogService>();
        catalog.GetBookAsync(bookId, Arg.Any<CancellationToken>())
            .Returns(new BookSnapshot(bookId, "Clean Architecture V2", 199.99m, "USD"));

        var existingOrder = Order.Create(request.CustomerId, key);
        existingOrder.AddItem(bookId, "Clean Architecture V2", new Money(199.99m, "USD"), 2);
        existingOrder.ChangeStatus(Orders.Domain.Enums.OrderStatus.Paid);
        existingOrder.ChangeStatus(Orders.Domain.Enums.OrderStatus.Shipped);

        var repository = Substitute.For<IOrderRepository>();
        repository.GetByIdempotencyKeyAsync(key, Arg.Any<CancellationToken>())
            .Returns(existingOrder);

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var command = new CreateOrderCommand(repository, unitOfWork, catalog);

        var result = await command.ExecuteAsync(request, key);

        result.Created.Should().BeFalse();
        result.Order.Id.Should().Be(existingOrder.Id.Value);
        result.Order.Status.Should().Be("Shipped");

        await repository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}