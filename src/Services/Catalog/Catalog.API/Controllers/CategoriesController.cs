using Catalog.Application.Commands;
using Catalog.Application.DTOs;
using Catalog.Application.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.API.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController(
    CreateCategoryCommand createCategoryCommand,
    GetCategoryByIdQuery getCategoryByIdQuery,
    IValidator<CreateCategoryRequest> createCategoryValidator)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await createCategoryValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            return ValidationProblem(ModelState);
        }

        var category = await createCategoryCommand.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var category = await getCategoryByIdQuery.ExecuteAsync(id, cancellationToken);
        return Ok(category);
    }
}