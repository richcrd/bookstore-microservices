using Orders.Application.DTOs;
using Orders.Application.Interfaces;

namespace Orders.Application.Queries;

public record PaginatedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public class GetOrdersQuery(IOrderRepository orderRepository)
{
    public async Task<PaginatedResult<OrderDto>> ExecuteAsync(
        Guid? customerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (orders, totalCount) = await orderRepository.GetOrdersAsync(customerId, page, pageSize, cancellationToken);

        var dtos = orders.Select(o => new OrderDto(
            o.Id.Value,
            o.CustomerId,
            o.Status.ToString(),
            o.Total.Amount,
            o.Total.Currency,
            o.CreatedAt,
            o.UpdatedAt,
            o.Items.Select(i => new OrderItemDto(
                i.BookId,
                i.Title,
                i.UnitPrice.Amount,
                i.UnitPrice.Currency,
                i.Quantity,
                i.LineTotal.Amount)).ToList())).ToList();

        return new PaginatedResult<OrderDto>(dtos, totalCount, page, pageSize);
    }
}