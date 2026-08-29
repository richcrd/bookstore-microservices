using Inventory.Application.Commands;
using Inventory.Application.DTOs;
using MassTransit;
using SharedKernel.Messages;

namespace Inventory.API.Consumers;

public class OrderCreatedConsumer(ReserveStockCommand reserveStockCommand) : IConsumer<OrderCreatedMessage>
{
    public async Task Consume(ConsumeContext<OrderCreatedMessage> context)
    {
        foreach (var item in context.Message.Items)
        {
            await reserveStockCommand.ExecuteAsync(
                item.BookId,
                new StockOperationRequest(item.Quantity),
                context.CancellationToken);
        }
    }
}