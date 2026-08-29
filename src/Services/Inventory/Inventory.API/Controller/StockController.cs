using FluentValidation;
using Inventory.Application.Commands;
using Inventory.Application.DTOs;
using Inventory.Application.Queries;
using Inventory.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace Inventory.API.Controller;

[Authorize]
[ApiController]
[Route("api/v1/stock-items")]
public class StockController(
    CreateStockCommand createStockCommand,
    AddStockCommand addStockCommand,
    ReserveStockCommand reserveStockCommand,
    ReleaseStockCommand releaseStockCommand,
    DeductStockCommand deductStockCommand,
    GetStockByBookQuery getStockByBookQuery,
    GetStockByIdQuery getStockByIdQuery,
    GetStocksQuery getStocksQuery,
    IValidator<CreateStockItemRequest> createStockValidator,
    IValidator<StockOperationRequest> operationValidator)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<StockItemDto>> Create([FromBody] CreateStockItemRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createStockValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                return ValidationProblem(ModelState);
            }
        }

        var stockItem = await createStockCommand.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByBookId), new { bookId = stockItem.BookId }, stockItem);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<StockItemDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var stockItem = await getStockByIdQuery.ExecuteAsync(new StockId(id), cancellationToken);
        return Ok(stockItem);
    }

    [HttpGet("by-book/{bookId:guid}")]
    public async Task<ActionResult<StockItemDto>> GetByBookId(Guid bookId, CancellationToken cancellationToken)
    {
        var stockItem = await getStockByBookQuery.ExecuteAsync(bookId, cancellationToken);
        return Ok(stockItem);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<StockItemDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await getStocksQuery.ExecuteAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{bookId:guid}/add")]
    public async Task<ActionResult<StockItemDto>> Add(
        Guid bookId,
        [FromBody] StockOperationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await operationValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                return ValidationProblem(ModelState);
            }
        }

        var stockItem = await addStockCommand.ExecuteAsync(bookId, request, cancellationToken);
        return Ok(stockItem);
    }

    [HttpPost("{bookId:guid}/reserve")]
    public async Task<ActionResult<StockItemDto>> Reserve(
        Guid bookId,
        [FromBody] StockOperationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await operationValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                return ValidationProblem(ModelState);
            }
        }

        var stockItem = await reserveStockCommand.ExecuteAsync(bookId, request, cancellationToken);
        return Ok(stockItem);
    }

    [HttpPost("{bookId:guid}/release")]
    public async Task<ActionResult<StockItemDto>> Release(
        Guid bookId,
        [FromBody] StockOperationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await operationValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                return ValidationProblem(ModelState);
            }
        }

        var stockItem = await releaseStockCommand.ExecuteAsync(bookId, request, cancellationToken);
        return Ok(stockItem);
    }

    [HttpPost("{bookId:guid}/deduct")]
    public async Task<ActionResult<StockItemDto>> Deduct(
        Guid bookId,
        [FromBody] StockOperationRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await operationValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                return ValidationProblem(ModelState);
            }
        }

        var stockItem = await deductStockCommand.ExecuteAsync(bookId, request, cancellationToken);
        return Ok(stockItem);
    }
}