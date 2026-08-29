using MassTransit;
using SharedKernel.Messages;

namespace OrderSaga.Worker;

public class PaymentGatewayConsumer : IConsumer<RequestPaymentCommand>
{
    public async Task Consume(ConsumeContext<RequestPaymentCommand> context)
    {
        await Task.Delay(TimeSpan.FromMilliseconds(250), context.CancellationToken);

        var succeeded = context.Message.Amount < 10000m;
        var reason = succeeded ? null : "Amount exceeds the payment gateway limit.";

        await context.RespondAsync(new PaymentCompleted(context.Message.OrderId, succeeded, reason));
    }
}