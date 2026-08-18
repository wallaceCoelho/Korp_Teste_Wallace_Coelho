using Domain.Entities;
using Domain.Tests.Fakers;
using Shouldly;
using Xunit;

namespace Domain.Tests.Entities;

public class CategoryTests
{
    [Fact]
    public void Create_WithValidName_ShouldReturnSuccessCategory()
    {
        // Arrange
        const string name = "Eletrônicos";

        // Act
        var result = Category.Create(name);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.Name.ShouldBe(name);
        result.Value.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
        result.Value.UpdatedAt.ShouldBeNull();
        result.Value.Products.ShouldBeEmpty();
    }

    [Fact]
    public void Create_WithLeadingAndTrailingWhitespace_ShouldTrimName()
    {
        // Arrange
        const string rawName = "   Informática   ";
        const string expectedName = "Informática";

        // Act
        var result = Category.Create(rawName);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Name.ShouldBe(expectedName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldReturnFailure(string? invalidName)
    {
        // Act
        var result = Category.Create(invalidName!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Error.ShouldBe("Nome da categoria é obrigatório.");
    }

    [Fact]
    public void UpdateName_WithValidNewName_ShouldUpdateNameAndTimestamp()
    {
        // Arrange
        var category = CategoryFaker.GenerateValid();
        const string newName = "  Novos Eletrônicos  ";
        const string expectedName = "Novos Eletrônicos";

        // Act
        var result = category.UpdateName(newName);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        category.Name.ShouldBe(expectedName);
        category.UpdatedAt.ShouldNotBeNull();
        category.UpdatedAt.Value.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateName_WithInvalidNewName_ShouldReturnFailure(string? invalidName)
    {
        // Arrange
        var category = CategoryFaker.GenerateValid();
        var originalName = category.Name;

        // Act
        var result = category.UpdateName(invalidName!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Nome da categoria é obrigatório.");
        category.Name.ShouldBe(originalName);
        category.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void CanDelete_WhenNoProductsAssociated_ShouldReturnSuccess()
    {
        // Arrange
        var category = CategoryFaker.GenerateValid();

        // Act
        var result = category.CanDelete();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBeNull();
    }
}
