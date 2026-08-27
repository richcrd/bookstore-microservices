using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Entities;

public class Category
{
    public CategoryId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    
    /// <summary>
    /// Required by Entity Framework Core for materializing the Entity
    /// </summary>
    private Category() { }

    public static Category Create(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        return new Category()
        {
            Id = CategoryId.New(),
            Name = name,
            Description = description
        };
    }

    public void Update(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        Name = name;
        Description = description;
    }
}
