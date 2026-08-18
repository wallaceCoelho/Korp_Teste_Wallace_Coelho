using Domain.Entities;
using Shouldly;
using Xunit;

namespace Domain.Tests.Entities;

public class InvoiceItemTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnSuccess()
    {
        // Arrange
        var productId = Guid.NewGuid();
        const string code = "PRD-001";
        const string desc = "Mouse Sem Fio";
        const int quantity = 3;
        const decimal price = 99.90m;

        // Act
        var result = InvoiceItem.Create(productId, code, desc, quantity, price);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.ProductId.ShouldBe(productId);
        result.Value.ProductCode.ShouldBe(code);
        result.Value.ProductDescription.ShouldBe(desc);
        result.Value.Quantity.ShouldBe(quantity);
        result.Value.UnitPrice.ShouldBe(price);
        result.Value.TotalPrice.ShouldBe(299.70m);
    }

    [Fact]
    public void Create_WithEmptyProductId_ShouldReturnFailure()
    {
        // Act
        var result = InvoiceItem.Create(Guid.Empty, "PRD-001", "Desc", 1, 10m);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("ID do produto inválido.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCode_ShouldReturnFailure(string? invalidCode)
    {
        // Act
        var result = InvoiceItem.Create(Guid.NewGuid(), invalidCode!, "Desc", 1, 10m);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Código do produto é obrigatório.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidDescription_ShouldReturnFailure(string? invalidDesc)
    {
        // Act
        var result = InvoiceItem.Create(Guid.NewGuid(), "PRD-001", invalidDesc!, 1, 10m);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Descrição do produto é obrigatória.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void Create_WithZeroOrNegativeQuantity_ShouldReturnFailure(int invalidQuantity)
    {
        // Act
        var result = InvoiceItem.Create(Guid.NewGuid(), "PRD-001", "Desc", invalidQuantity, 10m);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("A quantidade deve ser maior que zero.");
    }

    [Fact]
    public void Create_WithNegativePrice_ShouldReturnFailure()
    {
        // Act
        var result = InvoiceItem.Create(Guid.NewGuid(), "PRD-001", "Desc", 1, -5m);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("O preço unitário não pode ser negativo.");
    }
}
