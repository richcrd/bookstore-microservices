namespace SharedKernel.Messages;


public record OrderItemMessage(Guid BookId, int Quantity);

public record OrderCreatedMessage(
    Guid OrderId,
    Guid CustomerId,
    decimal Total,
    string Currency,
    DateTime OccurredOn,
    List<OrderItemMessage> Items);

public record OrderStatusChangedMessage(
    Guid OrderId,
    Guid CustomerId,
    string OldStatus,
    string NewStatus,
    DateTime OccurredOn,
    List<OrderItemMessage> Items);

public record RequestPaymentCommand(Guid OrderId, decimal Amount, string Currency);

public record PaymentCompleted(Guid OrderId, bool Succeeded, string? Reason);

public record ChangeOrderStatusCommand(Guid OrderId, string NewStatus);