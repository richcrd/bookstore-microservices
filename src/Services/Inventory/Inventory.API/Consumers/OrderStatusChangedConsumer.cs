using Inventory.Application.Commands;
using Inventory.Application.DTOs;
using MassTransit;
using SharedKernel.Messages;

namespace Inventory.API.Consumers;

public class OrderStatusChangedConsumer(DeductStockCommand deductStockCommand,
    ReleaseStockCommand releaseStockCommand) : IConsumer<OrderStatusChangedMessage>
{
    public async Task Consume(ConsumeContext<OrderStatusChangedMessage> context)
    {
        var message = context.Message;

        foreach (var item in message.Items)
        {
            switch (message.NewStatus)
            {
                case "Shipped":
                    await deductStockCommand.ExecuteAsync(
                        item.BookId,
                        new StockOperationRequest(item.Quantity),
                        context.CancellationToken);
                    break;
                
                case "Cancelled":
                    await releaseStockCommand.ExecuteAsync(
                        item.BookId,
                        new StockOperationRequest(item.Quantity),
                        context.CancellationToken);
                    break;
            }
        }
    }
}