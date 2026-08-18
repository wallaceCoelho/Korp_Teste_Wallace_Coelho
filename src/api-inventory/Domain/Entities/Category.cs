using Domain.Common;

namespace Domain.Entities;

public sealed class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }

    private readonly List<Product> _products = [];
    public IReadOnlyCollection<Product> Products => _products.AsReadOnly();

    private Category() { }

    public static DomainResult<Category> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Nome da categoria é obrigatório.";

        return DomainResult<Category>.Success(new Category
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            CreatedAt = DateTime.UtcNow
        });
    }

    public DomainResult UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return "Nome da categoria é obrigatório.";

        Name = newName.Trim();
        UpdatedAt = DateTime.UtcNow;

        return DomainResult.Success();
    }

    public DomainResult CanDelete()
    {
        return DomainResult.Success();
    }
}
