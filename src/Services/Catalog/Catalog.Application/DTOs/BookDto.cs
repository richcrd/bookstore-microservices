namespace Catalog.Application.DTOs;

public record BookDto(
    Guid Id,
    string Title,
    string Isbn,
    string Description,
    string Author,
    decimal Price,
    string Currency,
    DateTime CreatedAt,
    DateTime? UpdatedAt
    );