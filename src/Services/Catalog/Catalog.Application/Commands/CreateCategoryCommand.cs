using Catalog.Application.DTOs;
using Catalog.Application.Interfaces;
using Catalog.Domain.Entities;

namespace Catalog.Application.Commands;

public class CreateCategoryCommand(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
{
    public async Task<CategoryDto> ExecuteAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = Category.Create(request.Name, request.Description);

        await categoryRepository.AddAsync(category, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CategoryDto(category.Id.Value, category.Name, category.Description);
    }
}
