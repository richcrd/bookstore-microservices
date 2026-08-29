using Catalog.Application.Commands;
using Catalog.Application.DTOs;
using Catalog.Application.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/v1/books")]
public class BooksController(
    CreateBookCommand createBookCommand,
    UpdateBookPriceCommand updateBookPriceCommand,
    GetBookByIdQuery getBookByIdQuery,
    GetBooksQuery getBooksQuery,
    IValidator<CreateBookRequest> createBookValidator)
    : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BookDto>> Create([FromBody] CreateBookRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createBookValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                return ValidationProblem(ModelState);
            }
        }

        var book = await createBookCommand.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }
    
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<BookDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var book = await getBookByIdQuery.ExecuteAsync(id, cancellationToken);
        return Ok(book);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PaginatedResult<BookDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await getBooksQuery.ExecuteAsync(search, page, pageSize, cancellationToken);
        return Ok(result);
    }
    
    [HttpPatch("{id:guid}/price")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<BookDto>> UpdatePrice(
        Guid id,
        [FromBody] UpdateBookPriceRequest request,
        CancellationToken cancellationToken)
    {
        var book = await updateBookPriceCommand.ExecuteAsync(id, request.NewPrice, request.Currency, cancellationToken);
        return Ok(book);
    }
}