using Catalog.Application.DTOs;
using FluentValidation;

namespace Catalog.Application.Commands.Validation;

/// <summary>
/// Fluent Validation, better than if (string.IsNullOrEmpty) everywhere
/// </summary>
public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
        
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}