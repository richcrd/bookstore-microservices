using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using OrderSaga.Worker;
using SharedKernel.Messages;

namespace OrderSaga.UnitTests;

public class OrderSagaStateMachineTests
{
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid CustomerId = Guid.NewGuid();
    private static readonly Guid BookId = Guid.NewGuid();

    private static OrderCreatedMessage NewOrder(decimal total = 399.98m, string currency = "USD") =>
        new(OrderId, CustomerId, total, currency, DateTime.UtcNow,
            [new OrderItemMessage(BookId, 2)]);

    private static OrderStatusChangedMessage StatusChanged(string newStatus) =>
        new(OrderId, CustomerId, "Pending", newStatus, DateTime.UtcNow,
            [new OrderItemMessage(BookId, 2)]);

    [Fact]
    public async Task HappyPath_PaymentApproved_OrderReachesShipped()
    {
        await using var provider = CreateHarness();
        var harness = provider.GetTestHarness();
        await harness.Start();

        await harness.Bus.Publish(NewOrder());

        var paid = await harness.Published.Any<ChangeOrderStatusCommand>(
            x => x.Context.Message.NewStatus == "Paid");
        paid.Should().BeTrue();

        await harness.Bus.Publish(StatusChanged("Paid"));

        var shipped = await harness.Published.Any<ChangeOrderStatusCommand>(
            x => x.Context.Message.NewStatus == "Shipped");
        shipped.Should().BeTrue();

        await harness.Bus.Publish(StatusChanged("Shipped"));

        var requestConsumed = await harness.Consumed.Any<RequestPaymentCommand>();
        requestConsumed.Should().BeTrue();
        var statusHandled = await harness.Consumed.Any<OrderStatusChangedMessage>();
        statusHandled.Should().BeTrue();
        var cancelled = await harness.Published.Any<ChangeOrderStatusCommand>(
            x => x.Context.Message.NewStatus == "Cancelled");
        cancelled.Should().BeFalse();
    }

    [Fact]
    public async Task HappyPath_GatewaySplitsPaymentRequest()
    {
        await using var provider = CreateHarness();
        var harness = provider.GetTestHarness();
        await harness.Start();

        await harness.Bus.Publish(NewOrder());

        var requested = await harness.Consumed.Any<RequestPaymentCommand>();
        requested.Should().BeTrue();

        var responded = await harness.Consumed.Any<PaymentCompleted>();
        responded.Should().BeTrue();
    }

    [Fact]
    public async Task Rejection_AfterThreeRetries_OrderIsCancelled()
    {
        await using var provider = CreateHarness();
        var harness = provider.GetTestHarness();
        await harness.Start();

        await harness.Bus.Publish(NewOrder(total: 15000m));

        var cancelled = await harness.Published.Any<ChangeOrderStatusCommand>(
            x => x.Context.Message.NewStatus == "Cancelled");
        cancelled.Should().BeTrue();

        var paid = await harness.Published.Any<ChangeOrderStatusCommand>(
            x => x.Context.Message.NewStatus == "Paid");
        paid.Should().BeFalse();

        var requests = await harness.Consumed.SelectAsync<RequestPaymentCommand>().Count();
        requests.Should().Be(3);
    }

    private static ServiceProvider CreateHarness()
    {
        var services = new ServiceCollection();

        services.AddMassTransitTestHarness(x =>
        {
            x.AddSagaStateMachine<OrderSagaStateMachine, OrderSagaState>()
                .InMemoryRepository();

            x.AddConsumer<PaymentGatewayConsumer>();

            x.SetKebabCaseEndpointNameFormatter();

            x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
        });

        return services.BuildServiceProvider();
    }
}