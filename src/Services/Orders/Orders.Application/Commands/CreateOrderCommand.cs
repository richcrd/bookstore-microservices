using Orders.Application.DTOs;
using Orders.Application.Interfaces;
using Orders.Domain.Entities;
using Orders.Domain.ValueObjects;

namespace Orders.Application;

public class CreateOrderCommand(IOrderRepository orderRepository, IUnitOfWork unitOfWork, ICatalogService catalogService)
{
    public async Task<OrderDto> ExecuteAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = Order.Create(request.CustomerId);

        foreach (var item in request.Items)
        {
            var book = await catalogService.GetBookAsync(item.BookId, cancellationToken);
            order.AddItem(book.Id, book.Title, new Money(book.Price, book.Currency), item.Quantity);
        }

        await orderRepository.AddAsync(order, cancellationToken);
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