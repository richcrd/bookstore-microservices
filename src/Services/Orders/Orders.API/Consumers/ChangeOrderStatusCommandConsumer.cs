using MassTransit;
using Orders.Application.Commands;
using SharedKernel.Messages;

namespace Orders.API.Consumers;

public class ChangeOrderStatusCommandConsumer(UpdateOrderStatusCommand updateOrderStatusCommand)
    : IConsumer<ChangeOrderStatusCommand>
{
    public async Task Consume(ConsumeContext<ChangeOrderStatusCommand> context)
    {
        await updateOrderStatusCommand.ExecuteAsync(
            context.Message.OrderId,
            context.Message.NewStatus,
            context.CancellationToken);
    }
}