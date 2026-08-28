using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;

namespace Catalog.Application.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    void Update(Category category);

    void Remove(Category category);
}
