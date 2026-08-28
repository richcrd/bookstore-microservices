namespace Catalog.Application.DTOs;

public record CreateBookRequest(
    string Title,
    string Isbn,
    string Description,
    string Author,
    decimal Price,
    string Currency
    );