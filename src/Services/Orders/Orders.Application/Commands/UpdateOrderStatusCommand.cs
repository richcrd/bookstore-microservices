using Orders.Application.DTOs;
using Orders.Application.Interfaces;
using Orders.Domain.Entities;
using Orders.Domain.Enums;
using Orders.Domain.Exceptions;
using Orders.Domain.ValueObjects;


namespace Orders.Application.Commands;

public class UpdateOrderStatusCommand(IOrderRepository orderRepository, IUnitOfWork unitOfWork)
{
    public async Task<OrderDto> ExecuteAsync(Guid orderId, string newStatus, CancellationToken cancellationToken = default)
    {
        var order = await orderRepository.GetByIdAsync(new OrderId(orderId), cancellationToken)
                    ?? throw new OrderNotFoundException($"Order {orderId} not found.");

        if (!Enum.TryParse<OrderStatus>(newStatus, ignoreCase: true, out var status))
        {
            throw new OrderDomainException($"'{newStatus}' is not a valid order status.");
        }

        order.ChangeStatus(status);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order order) => new OrderDto(
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