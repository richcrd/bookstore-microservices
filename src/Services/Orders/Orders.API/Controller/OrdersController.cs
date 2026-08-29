using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Orders.Application;
using Orders.Application.Commands;
using Orders.Application.DTOs;
using Orders.Application.Queries;

namespace Orders.API.Controller;

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

        var order = await createOrderCommand.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
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
