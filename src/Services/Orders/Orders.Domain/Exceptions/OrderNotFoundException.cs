namespace Orders.Domain.Exceptions;

public class OrderNotFoundException(string message) : OrderDomainException(message);