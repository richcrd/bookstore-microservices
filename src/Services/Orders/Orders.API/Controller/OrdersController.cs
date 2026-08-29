using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orders.Application;
using Orders.Application.Commands;
using Orders.Application.DTOs;
using Orders.Application.Queries;

namespace Orders.API.Controller;

[Authorize]
[ApiController]
[Route("api/v1/orders")]
public class OrdersController(
    CreateOrderCommand createOrderCommand,
    GetOrderByIdQuery getOrderByIdQuery,
    GetOrdersQuery getOrdersQuery,
    IValidator<CreateOrderRequest> createOrderValidator,
    UpdateOrderStatusCommand updateOrderStatusCommand
    ) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createOrderValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                return ValidationProblem(ModelState);
            }
        }

        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(idempotencyKey) && !Guid.TryParse(idempotencyKey, out _))
        {
            return ValidationProblem("Idempotency-Key must be a valid GUID.");
        }

        var result = await createOrderCommand.ExecuteAsync(request, idempotencyKey, cancellationToken);

        return result.Created
            ? CreatedAtAction(nameof(GetById), new { id = result.Order.Id }, result.Order)
            : Ok(result.Order);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await getOrderByIdQuery.ExecuteAsync(id, cancellationToken);
        return Ok(order);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<OrderDto>>> GetAll(
        [FromQuery] Guid? customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await getOrdersQuery.ExecuteAsync(customerId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var order = await updateOrderStatusCommand.ExecuteAsync(id, request.Status, cancellationToken);
        return Ok(order);
    }
}
