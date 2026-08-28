namespace Catalog.Application.DTOs;

public record CreateCategoryRequest(
    string Name,
    string Description
    );