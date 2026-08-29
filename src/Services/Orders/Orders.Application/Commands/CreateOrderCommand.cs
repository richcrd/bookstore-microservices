using Orders.Application.DTOs;
using Orders.Application.Interfaces;
using Orders.Domain.Entities;
using Orders.Domain.ValueObjects;

namespace Orders.Application;

public class CreateOrderCommand(IOrderRepository orderRepository, IUnitOfWork unitOfWork, ICatalogService catalogService)
{
    public async Task<CreateOrderResult> ExecuteAsync(
        CreateOrderRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = string.IsNullOrWhiteSpace(idempotencyKey)
            ? Guid.NewGuid().ToString("D")
            : idempotencyKey;

        var existing = await orderRepository.GetByIdempotencyKeyAsync(key, cancellationToken);
        if (existing is not null)
        {
            return new CreateOrderResult(MapToDto(existing), Created: false);
        }

        var order = Order.Create(request.CustomerId, key);

        foreach (var item in request.Items)
        {
            var book = await catalogService.GetBookAsync(item.BookId, cancellationToken);
            order.AddItem(book.Id, book.Title, new Money(book.Price, book.Currency), item.Quantity);
        }

        await orderRepository.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateOrderResult(MapToDto(order), Created: true);
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