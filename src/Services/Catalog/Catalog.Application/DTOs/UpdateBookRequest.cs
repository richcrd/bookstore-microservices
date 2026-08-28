namespace Catalog.Application.DTOs;

public record UpdateBookRequest(
    string Title,
    string Description,
    string Author
    );