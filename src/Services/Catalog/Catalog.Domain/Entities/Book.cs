using Catalog.Domain.Events;
using Catalog.Domain.ValueObjects;

namespace Catalog.Domain.Entities;

public class Book
{
    private readonly List<BookCategory> _bookCategories = [];
    private readonly List<IDomainEvent> _domainEvents = [];
    
    public BookId Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public Isbn Isbn { get; private set; } = null!;
    public string Description { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public Money Price { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyList<BookCategory> BookCategories => _bookCategories;
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    /// <summary>
    /// Required by Entity Framework Core for materializing the Entity
    /// </summary>
    private Book() { }

    public static Book Create(
        string title,
        Isbn isbn,
        string description,
        string author,
        Money price)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Book title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(author))
        {
            throw new ArgumentException("Author is required.", nameof(author));
        }

        var book = new Book()
        {
            Id = BookId.New(),
            Title = title,
            Isbn = isbn,
            Description = description,
            Author = author,
            Price = price,
            CreatedAt = DateTime.UtcNow
        };
        
        book._domainEvents.Add(new BookCreated(book.Id.Value, book.Title, book.Isbn.Value));

        return book;
    }

    public void UpdatePrice(Money newPrice)
    {
        if (newPrice.Currency != Price.Currency)
        {
            throw new InvalidOperationException("Cannot change currency. Create a new book instead.");
        }

        var oldPrice = Price;
        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
        
        _domainEvents.Add(new BookPriceChanged(Id.Value, oldPrice.Amount, newPrice.Amount, newPrice.Currency));
    }

    public void UpdateDetails(string title, string description, string author)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Book title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));

        Title = title;
        Description = description;
        Author = author;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddCategory(CategoryId categoryId)
    {
        if (_bookCategories.Any(bc => bc.CategoryId == categoryId))
            return;
        
        _bookCategories.Add(new BookCategory(Id, categoryId));
    }

    public void RemoveCategory(CategoryId categoryId)
    {
        var bookCategory = _bookCategories.FirstOrDefault(bc => bc.CategoryId == categoryId);
        if (bookCategory is not null)
            _bookCategories.Remove(bookCategory);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
