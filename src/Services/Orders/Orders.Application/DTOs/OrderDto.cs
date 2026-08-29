namespace Orders.Application.DTOs;

public record OrderItemDto(
    Guid BookId,
    string Title,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal LineTotal);

public record OrderDto(
    Guid Id,
    Guid CustomerId,
    string Status,
    decimal Total,
    string Currency,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<OrderItemDto> Items);