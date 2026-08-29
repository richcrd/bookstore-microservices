using FluentAssertions;
using NSubstitute;
using Orders.Application.Commands;
using Orders.Application.Interfaces;
using Orders.Domain.Entities;
using Orders.Domain.Enums;
using Orders.Domain.Events;
using Orders.Domain.Exceptions;
using Orders.Domain.ValueObjects;

namespace Orders.UnitTests.Application;

public class UpdateOrderStatusCommandTests
{
    private readonly IOrderRepository _repository = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateOrderStatusCommand _command;
    private readonly Order _order;

    public UpdateOrderStatusCommandTests()
    {
        _order = Order.Create(Guid.NewGuid(), Guid.NewGuid().ToString("D"));
        _order.AddItem(Guid.NewGuid(), "Clean Architecture V2", new Money(199.99m, "USD"), 2);

        _repository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(_order));

        _command = new UpdateOrderStatusCommand(_repository, _unitOfWork);
    }

    [Fact]
    public async Task Execute_ValidTransition_UpdatesStatusAndEmitsEvent()
    {
        var dto = await _command.ExecuteAsync(_order.Id.Value, "Paid", CancellationToken.None);

        dto.Status.Should().Be("Paid");
        _order.Status.Should().Be(OrderStatus.Paid);
        _order.DomainEvents.OfType<OrderStatusChanged>()
            .Should().ContainSingle(e => e.NewStatus == OrderStatus.Paid);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_PendingToPaidToShipped_EmitsTwoEvents()
    {
        await _command.ExecuteAsync(_order.Id.Value, "Paid", CancellationToken.None);
        await _command.ExecuteAsync(_order.Id.Value, "Shipped", CancellationToken.None);

        _order.Status.Should().Be(OrderStatus.Shipped);
        _order.DomainEvents.OfType<OrderStatusChanged>().Should().HaveCount(2);

        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_InvalidStatusText_ThrowsOrderDomainException()
    {
        var act = async () => await _command.ExecuteAsync(_order.Id.Value, "Hacked", CancellationToken.None);

        await act.Should().ThrowAsync<OrderDomainException>();
        await _unitOfWork.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_NonexistentOrder_ThrowsOrderNotFoundException()
    {
        _repository.GetByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Order?>(null));

        var act = async () => await _command.ExecuteAsync(Guid.NewGuid(), "Paid", CancellationToken.None);

        await act.Should().ThrowAsync<OrderNotFoundException>();
        await _unitOfWork.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Execute_IllegalTransition_ThrowsInvalidOperationException()
    {
        var act = async () => await _command.ExecuteAsync(_order.Id.Value, "Shipped", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _order.Status.Should().Be(OrderStatus.Pending);
        await _unitOfWork.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}