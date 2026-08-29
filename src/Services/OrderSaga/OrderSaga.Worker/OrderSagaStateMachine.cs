using MassTransit;
using SharedKernel.Messages;

namespace OrderSaga.Worker;

public class OrderSagaStateMachine : MassTransitStateMachine<OrderSagaState>
{
    private const int MaxPaymentAttempts = 3;

    public State AwaitingPayment { get; private set; } = default!;
    public State PaymentApproved { get; private set; } = default!;
    public State ShipmentRequested { get; private set; } = default!;
    public State Completed { get; private set; } = default!;
    public State Cancelled { get; private set; } = default!;

    public Event<OrderCreatedMessage> OrderPlaced { get; private set; } = default!;
    public Event<OrderStatusChangedMessage> OrderStatusChanged { get; private set; } = default!;
    public Request<OrderSagaState, RequestPaymentCommand, PaymentCompleted> Payment { get; private set; } = default!;

    public OrderSagaStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderPlaced, e => e
            .CorrelateBy((saga, context) => saga.OrderId == context.Message.OrderId)
            .SelectId(context => context.Message.OrderId));

        Event(() => OrderStatusChanged, e => e
            .CorrelateBy((saga, context) => saga.OrderId == context.Message.OrderId));

        Request(() => Payment, r =>
        {
            r.ServiceAddress = new Uri("queue:payment-gateway");
            r.Timeout = TimeSpan.Zero;
        });

        Initially(
            When(OrderPlaced)
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.OrderId;
                    context.Saga.OrderId = context.Message.OrderId;
                    context.Saga.Amount = context.Message.Total;
                    context.Saga.Currency = context.Message.Currency;
                    context.Saga.PaymentAttempts = 1;
                    context.Saga.CreatedAt = DateTime.UtcNow;
                })
                .Request(Payment, context => new RequestPaymentCommand(
                    context.Saga.OrderId, context.Saga.Amount, context.Saga.Currency))
                .TransitionTo(AwaitingPayment));

        During(AwaitingPayment,
            When(Payment.Completed)
                .IfElse(context => context.Message.Succeeded,
                    then => then
                        .Publish(context => new ChangeOrderStatusCommand(context.Saga.OrderId, "Paid"))
                        .TransitionTo(PaymentApproved),
                    @else => @else
                        .IfElse(context => context.Saga.PaymentAttempts < MaxPaymentAttempts,
                            retry => retry
                                .Then(context => context.Saga.PaymentAttempts += 1)
                                .Request(Payment, context => new RequestPaymentCommand(
                                    context.Saga.OrderId, context.Saga.Amount, context.Saga.Currency)),
                            abort => abort
                                .Publish(context => new ChangeOrderStatusCommand(context.Saga.OrderId, "Cancelled"))
                                .TransitionTo(Cancelled))),
            When(Payment.Faulted)
                .IfElse(context => context.Saga.PaymentAttempts < MaxPaymentAttempts,
                    retry => retry
                        .Then(context => context.Saga.PaymentAttempts += 1)
                        .Request(Payment, context => new RequestPaymentCommand(
                            context.Saga.OrderId, context.Saga.Amount, context.Saga.Currency)),
                    abort => abort
                        .Publish(context => new ChangeOrderStatusCommand(context.Saga.OrderId, "Cancelled"))
                        .TransitionTo(Cancelled)));

        During(PaymentApproved,
            When(OrderStatusChanged, context => context.Message.NewStatus == "Paid")
                .Publish(context => new ChangeOrderStatusCommand(context.Saga.OrderId, "Shipped"))
                .TransitionTo(ShipmentRequested));

        During(ShipmentRequested,
            When(OrderStatusChanged, context => context.Message.NewStatus == "Shipped")
                .TransitionTo(Completed)
                .Finalize());

        During(Cancelled,
            When(OrderStatusChanged, context => context.Message.NewStatus == "Cancelled")
                .Finalize());

        SetCompletedWhenFinalized();
    }
}