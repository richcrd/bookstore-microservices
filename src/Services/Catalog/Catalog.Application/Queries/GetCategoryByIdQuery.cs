using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Exceptions;
using Catalog.Domain.ValueObjects;

namespace Catalog.Application.Queries;

public class GetCategoryByIdQuery(ICategoryRepository categoryRepository)
{
    public async Task<CategoryDto> ExecuteAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetByIdAsync(new CategoryId(categoryId), cancellationToken)
                       ?? throw new CatalogNotFoundException($"Category with ID {categoryId} not found.");

        return new CategoryDto(category.Id.Value, category.Name, category.Description);
    }
}