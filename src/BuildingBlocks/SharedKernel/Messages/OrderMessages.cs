namespace SharedKernel.Messages;


public record OrderItemMessage(Guid BookId, int Quantity);

public record OrderCreatedMessage(
    Guid OrderId,
    Guid CustomerId,
    DateTime OccurredOn,
    List<OrderItemMessage> Items);

public record OrderStatusChangedMessage(
    Guid OrderId,
    Guid CustomerId,
    string OldStatus,
    string NewStatus,
    DateTime OccurredOn,
    List<OrderItemMessage> Items);