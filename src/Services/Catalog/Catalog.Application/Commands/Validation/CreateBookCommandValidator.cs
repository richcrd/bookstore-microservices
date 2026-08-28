using Catalog.Application.DTOs;
using FluentValidation;

namespace Catalog.Application.Commands.Validation;

public class CreateBookRequestValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
        
        RuleFor(x => x.Isbn)
            .NotEmpty().WithMessage("ISBN is required.")
            .Matches(@"^[\d\-]+$").WithMessage("ISBN must contain only digits and hyphens.");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required.")
            .MaximumLength(150).WithMessage("Author must not exceed 150 characters.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter code (e.g., USD).");
    }
}