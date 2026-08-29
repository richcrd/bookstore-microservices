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

public class OrderCreatedConsumerTests
{
    private static readonly Guid BookId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();

    [Fact]
    public async Task Consume_OrderCreated_ReservesStockForOrderItems()
    {
        var stock = StockItem.Create(BookId, 10);

        var repository = Substitute.For<IStockItemRepository>();
        repository.GetByBookIdAsync(BookId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(stock));

        var unitOfWork = Substitute.For<IUnitOfWork>();

        await using var provider = CreateHarness(repository, unitOfWork);
        var harness = provider.GetTestHarness();
        await harness.Start();

        await harness.Bus.Publish(new OrderCreatedMessage(
            OrderId,
            CustomerId,
            399.98m,
            "USD",
            DateTime.UtcNow,
            [new OrderItemMessage(BookId, 2)]));

        var consumed = await harness.Consumed.Any<OrderCreatedMessage>();
        consumed.Should().BeTrue();

        stock.ReservedQuantity.Should().Be(2);
        stock.Available.Should().Be(8);

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consume_OrderCreated_NoStockForBook_ThrowsNotFoundException()
    {
        var stock = StockItem.Create(BookId, 5);

        var repository = Substitute.For<IStockItemRepository>();
        repository.GetByBookIdAsync(BookId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<StockItem?>(null));

        var unitOfWork = Substitute.For<IUnitOfWork>();

        await using var provider = CreateHarness(repository, unitOfWork);
        var harness = provider.GetTestHarness();
        await harness.Start();

        await harness.Bus.Publish(new OrderCreatedMessage(
            OrderId,
            CustomerId,
            399.98m,
            "USD",
            DateTime.UtcNow,
            [new OrderItemMessage(BookId, 2)]));

        var consumed = await harness.Consumed.Any<OrderCreatedMessage>();
        consumed.Should().BeTrue();

        stock.ReservedQuantity.Should().Be(0);
        await unitOfWork.Received(0).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static ServiceProvider CreateHarness(IStockItemRepository repository, IUnitOfWork unitOfWork)
    {
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