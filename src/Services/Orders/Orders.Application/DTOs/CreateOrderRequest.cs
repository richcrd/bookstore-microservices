namespace Orders.Application.DTOs;

public record CreateOrderRequest(Guid CustomerId, IReadOnlyList<CreateOrderItemRequest> Items);
public record CreateOrderItemRequest(Guid BookId, int Quantity);
