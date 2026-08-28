namespace Catalog.Application.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string Description
    );