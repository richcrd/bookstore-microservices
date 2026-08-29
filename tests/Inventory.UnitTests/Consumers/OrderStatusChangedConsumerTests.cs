using FluentAssertions;
using Inventory.API.Consumers;
using Inventory.Application.Commands;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SharedKernel.Messages;

namespace Inventory.UnitTests.Consumers;

public class OrderStatusChangedConsumerTests
{
    private static readonly Guid BookId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    [Fact]
    public async Task Consume_Shipped_DeductsReservedStock()
    {
        var stock = StockItem.Create(BookId, 10);
        stock.Reserve(5);
        stock.ReservedQuantity.Should().Be(5);

        await using var provider = CreateHarness(stock);
        var harness = provider.GetTestHarness();
        await harness.Start();

        await harness.Bus.Publish(new OrderStatusChangedMessage(
            OrderId,
            CustomerId,
            "Paid",
            "Shipped",
            DateTime.UtcNow,
            [new OrderItemMessage(BookId, 2)]));

        var consumed = await harness.Consumed.Any<OrderStatusChangedMessage>();
        consumed.Should().BeTrue();

        stock.ReservedQuantity.Should().Be(3);
        stock.QuantityOnHand.Should().Be(8);
        stock.Available.Should().Be(5);
    }

    [Fact]
    public async Task Consume_Cancelled_ReleasesReservedStock()
    {
        var stock = StockItem.Create(BookId, 10);
        stock.Reserve(7);
        stock.ReservedQuantity.Should().Be(7);

        await using var provider = CreateHarness(stock);
        var harness = provider.GetTestHarness();
        await harness.Start();

        await harness.Bus.Publish(new OrderStatusChangedMessage(
            OrderId,
            CustomerId,
            "Pending",
            "Cancelled",
            DateTime.UtcNow,
            [new OrderItemMessage(BookId, 2)]));

        var consumed = await harness.Consumed.Any<OrderStatusChangedMessage>();
        consumed.Should().BeTrue();

        stock.ReservedQuantity.Should().Be(5);
        stock.QuantityOnHand.Should().Be(10);
        stock.Available.Should().Be(5);
    }

    [Fact]
    public async Task Consume_UnhandledStatus_DoesNothing()
    {
        var stock = StockItem.Create(BookId, 10);
        stock.Reserve(4);

        var unitOfWork = Substitute.For<IUnitOfWork>();

        await using var provider = CreateHarness(stock, unitOfWork);
        var harness = provider.GetTestHarness();
        await harness.Start();

        await harness.Bus.Publish(new OrderStatusChangedMessage(
            OrderId,
            CustomerId,
            "Shipped",
            "Delivered",
            DateTime.UtcNow,
            [new OrderItemMessage(BookId, 2)]));

        var consumed = await harness.Consumed.Any<OrderStatusChangedMessage>();
        consumed.Should().BeTrue();

        stock.ReservedQuantity.Should().Be(4);
        stock.QuantityOnHand.Should().Be(10);

        await unitOfWork.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static ServiceProvider CreateHarness(StockItem stock, IUnitOfWork? unitOfWork = null)
    {
        var repository = Substitute.For<IStockItemRepository>();
        repository.GetByBookIdAsync(BookId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(stock));

        unitOfWork ??= Substitute.For<IUnitOfWork>();

        var services = new ServiceCollection();
        services.AddSingleton(repository);
        services.AddSingleton(unitOfWork);
        services.AddScoped<ReserveStockCommand>();
        services.AddScoped<DeductStockCommand>();
        services.AddScoped<ReleaseStockCommand>();

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<OrderCreatedConsumer>();
            x.AddConsumer<OrderStatusChangedConsumer>();
            x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });

        return services.BuildServiceProvider();
    }
}