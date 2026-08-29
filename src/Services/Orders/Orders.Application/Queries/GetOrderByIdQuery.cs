using Orders.Application.DTOs;
using Orders.Application.Interfaces;
using Orders.Domain.Entities;
using Orders.Domain.Exceptions;
using Orders.Domain.ValueObjects;

namespace Orders.Application.Queries;

public class GetOrderByIdQuery(IOrderRepository orderRepository)
{
    public async Task<OrderDto> ExecuteAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(orderId), cancellationToken)
                    ?? throw new OrderNotFoundException($"Order with ID {orderId} not found.");

        return MapToDto(order);
    }
    
    private static OrderDto MapToDto(Order order) => new(
        order.Id.Value,
        order.CustomerId,
        order.Status.ToString(),
        order.Total.Amount,
        order.Total.Currency,
        order.CreatedAt,
        order.UpdatedAt,
        order.Items.Select(i => new OrderItemDto(
            i.BookId,
            i.Title,
            i.UnitPrice.Amount,
            i.UnitPrice.Currency,
            i.Quantity,
            i.LineTotal.Amount)).ToList());
}