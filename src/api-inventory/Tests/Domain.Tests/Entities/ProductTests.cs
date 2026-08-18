using Domain.Entities;
using Domain.Tests.Fakers;
using Shouldly;
using Xunit;

namespace Domain.Tests.Entities;

public class ProductTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnSuccessProduct()
    {
        // Arrange
        const string code = "PRD-001";
        const string name = "Notebook Gamer";
        const string description = "Intel i7, 16GB RAM, SSD 512GB";
        const int stock = 15;
        const decimal price = 4500.50m;
        const int minStock = 5;

        // Act
        var result = Product.Create(code, name, stock, price, description, minStock);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.Code.ShouldBe(code);
        result.Value.Name.ShouldBe(name);
        result.Value.Description.ShouldBe(description);
        result.Value.StockQuantity.ShouldBe(stock);
        result.Value.UnitPrice.ShouldBe(price);
        result.Value.MinStockQuantity.ShouldBe(minStock);
        result.Value.CategoryId.ShouldBeNull();
        result.Value.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
        result.Value.UpdatedAt.ShouldBeNull();
        result.Value.DeletedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_WithNullDescription_ShouldReturnSuccessProductWithNullDescription()
    {
        // Arrange
        const string code = "PRD-002";
        const string name = "Mouse Sem Fio";

        // Act
        var result = Product.Create(code, name, 10, 50.00m, description: null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Description.ShouldBeNull();
    }

    [Fact]
    public void Create_WithLowercaseCodeAndExtraWhitespace_ShouldUppercaseAndTrim()
    {
        // Arrange
        const string rawCode = "  prd-123  ";
        const string rawName = "   Teclado Mecânico   ";

        // Act
        var result = Product.Create(rawCode, rawName, 10, 150.00m);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.Code.ShouldBe("PRD-123");
        result.Value.Name.ShouldBe("Teclado Mecânico");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidCode_ShouldReturnFailure(string? invalidCode)
    {
        // Act
        var result = Product.Create(invalidCode!, "Nome Válido", 10, 50m);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Código é obrigatório.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldReturnFailure(string? invalidName)
    {
        // Act
        var result = Product.Create("PRD-001", invalidName!, 10, 50m);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Nome do produto é obrigatório.");
    }

    [Fact]
    public void Create_WithNegativeUnitPrice_ShouldReturnFailure()
    {
        // Act
        var result = Product.Create("PRD-001", "Produto Teste", 10, -5m);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("O preço unitário não pode ser negativo.");
    }

    [Fact]
    public void Create_WithNegativeInitialStock_ShouldReturnFailure()
    {
        // Act
        var result = Product.Create("PRD-001", "Produto Teste", -1, 50m);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Quantidade inicial em estoque não pode ser negativa.");
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdatePropertiesAndTimestamp()
    {
        // Arrange
        var product = ProductFaker.GenerateValid();
        const string newCode = "  prd-999  ";
        const string newName = "  Novo Monitor  ";
        const string newDesc = "  Monitor UltraWide 29 polegadas  ";
        const decimal newPrice = 1200m;
        const int newMinStock = 2;

        // Act
        var result = product.UpdateDetails(newCode, newName, newPrice, newDesc, newMinStock);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.Code.ShouldBe("PRD-999");
        product.Name.ShouldBe("Novo Monitor");
        product.Description.ShouldBe("Monitor UltraWide 29 polegadas");
        product.UnitPrice.ShouldBe(newPrice);
        product.MinStockQuantity.ShouldBe(newMinStock);
        product.UpdatedAt.ShouldNotBeNull();
        product.UpdatedAt.Value.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public void UpdateDetails_WhenProductIsDeleted_ShouldReturnFailure()
    {
        // Arrange
        var product = ProductFaker.GenerateValid();
        product.Delete();

        // Act
        var result = product.UpdateDetails("PRD-002", "Novo Nome", 100m);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Não é possível atualizar um produto excluído.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithInvalidCode_ShouldReturnFailure(string? invalidCode)
    {
        // Arrange
        var product = ProductFaker.GenerateValid();

        // Act
        var result = product.UpdateDetails(invalidCode!, "Novo Nome", 100m);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Código é obrigatório.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDetails_WithInvalidName_ShouldReturnFailure(string? invalidName)
    {
        // Arrange
        var product = ProductFaker.GenerateValid();

        // Act
        var result = product.UpdateDetails("PRD-001", invalidName!, 100m);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Nome do produto é obrigatório.");
    }

    [Fact]
    public void UpdateDetails_WithNegativeUnitPrice_ShouldReturnFailure()
    {
        // Arrange
        var product = ProductFaker.GenerateValid();

        // Act
        var result = product.UpdateDetails("PRD-001", "Nome Válido", -10m);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("O preço unitário não pode ser negativo.");
    }

    [Fact]
    public void DeductStock_WithValidQuantity_ShouldDeductStock()
    {
        // Arrange
        var product = ProductFaker.CreateFaker(initialStock: 20).Generate();

        // Act
        var result = product.DeductStock(5);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.StockQuantity.ShouldBe(15);
        product.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void DeductStock_WithQuantityEqualToCurrentStock_ShouldZeroStock()
    {
        // Arrange
        var product = ProductFaker.CreateFaker(initialStock: 10).Generate();

        // Act
        var result = product.DeductStock(10);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.StockQuantity.ShouldBe(0);
    }

    [Fact]
    public void DeductStock_WhenProductIsDeleted_ShouldReturnFailure()
    {
        // Arrange
        var product = ProductFaker.CreateFaker(initialStock: 10).Generate();
        product.Delete();

        // Act
        var result = product.DeductStock(5);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Não é possível atualizar um produto excluído.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void DeductStock_WithZeroOrNegativeQuantity_ShouldReturnFailure(int invalidQuantity)
    {
        // Arrange
        var product = ProductFaker.CreateFaker(initialStock: 10).Generate();

        // Act
        var result = product.DeductStock(invalidQuantity);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("A quantidade a deduzir deve ser maior que zero.");
    }

    [Fact]
    public void DeductStock_WithQuantityGreaterThanAvailableStock_ShouldReturnFailure()
    {
        // Arrange
        var product = ProductFaker.CreateFaker(code: "PRD-TEST", initialStock: 5).Generate();

        // Act
        var result = product.DeductStock(10);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Estoque insuficiente para o produto 'PRD-TEST'. Disponível: 5, Solicitado: 10.");
    }

    [Fact]
    public void AddStock_WithValidQuantity_ShouldIncreaseStock()
    {
        // Arrange
        var product = ProductFaker.CreateFaker(initialStock: 10).Generate();

        // Act
        var result = product.AddStock(15);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.StockQuantity.ShouldBe(25);
        product.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void AddStock_WhenProductIsDeleted_ShouldReturnFailure()
    {
        // Arrange
        var product = ProductFaker.CreateFaker(initialStock: 10).Generate();
        product.Delete();

        // Act
        var result = product.AddStock(5);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Não é possível atualizar um produto excluído.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void AddStock_WithZeroOrNegativeQuantity_ShouldReturnFailure(int invalidQuantity)
    {
        // Arrange
        var product = ProductFaker.CreateFaker(initialStock: 10).Generate();

        // Act
        var result = product.AddStock(invalidQuantity);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("A quantidade a adicionar deve ser maior que zero.");
    }

    [Fact]
    public void ChangeCategory_WithValidGuid_ShouldUpdateCategoryId()
    {
        // Arrange
        var product = ProductFaker.GenerateValid();
        var categoryId = Guid.NewGuid();

        // Act
        var result = product.ChangeCategory(categoryId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.CategoryId.ShouldBe(categoryId);
        product.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void ChangeCategory_WhenProductIsDeleted_ShouldReturnFailure()
    {
        // Arrange
        var product = ProductFaker.GenerateValid();
        product.Delete();

        // Act
        var result = product.ChangeCategory(Guid.NewGuid());

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Não é possível atualizar um produto excluído.");
    }

    [Theory]
    [InlineData(null)]
    public void ChangeCategory_WithNullGuid_ShouldReturnFailure(Guid? nullId)
    {
        // Arrange
        var product = ProductFaker.GenerateValid();

        // Act
        var result = product.ChangeCategory(nullId);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("ID de categoria inválido.");
    }

    [Fact]
    public void ChangeCategory_WithEmptyGuid_ShouldReturnFailure()
    {
        // Arrange
        var product = ProductFaker.GenerateValid();

        // Act
        var result = product.ChangeCategory(Guid.Empty);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("ID de categoria inválido.");
    }

    [Fact]
    public void Delete_ShouldSetDeletedAtAndUpdatedAt()
    {
        // Arrange
        var product = ProductFaker.GenerateValid();

        // Act
        product.Delete();

        // Assert
        product.DeletedAt.ShouldNotBeNull();
        product.DeletedAt.Value.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
        product.UpdatedAt.ShouldNotBeNull();
        product.UpdatedAt.Value.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public void ReserveStock_WithSufficientStock_ShouldCreatePendingReservationAndReduceAvailableStock()
    {
        // Arrange
        var product = ProductFaker.CreateFaker(initialStock: 10).Generate();
        var invoiceId = Guid.NewGuid();

        // Act
        var result = product.ReserveStock(invoiceId, 4, TimeSpan.FromMinutes(30));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.InvoiceId.ShouldBe(invoiceId);
        result.Value.Quantity.ShouldBe(4);
        result.Value.Status.ShouldBe(Domain.Enums.ReservationStatus.Pending);
        product.StockQuantity.ShouldBe(10);
        product.AvailableStockQuantity.ShouldBe(6);
    }

    [Fact]
    public void ReserveStock_WithInsufficientStock_ShouldReturnFailure()
    {
        // Arrange
        var product = ProductFaker.CreateFaker(code: "PRD-100", initialStock: 3).Generate();
        var invoiceId = Guid.NewGuid();

        // Act
        var result = product.ReserveStock(invoiceId, 5, TimeSpan.FromMinutes(30));

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Estoque insuficiente para o produto 'PRD-100'. Disponível: 3, Solicitado: 5.");
        product.AvailableStockQuantity.ShouldBe(3);
    }

    [Fact]
    public void ConfirmReservationAndDeduct_WithPendingReservation_ShouldDeductStockAndConfirmReservation()
    {
        // Arrange
        var product = ProductFaker.CreateFaker(initialStock: 10).Generate();
        var invoiceId = Guid.NewGuid();
        product.ReserveStock(invoiceId, 4, TimeSpan.FromMinutes(30));

        // Act
        var result = product.ConfirmReservationAndDeduct(invoiceId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.StockQuantity.ShouldBe(6);
        product.AvailableStockQuantity.ShouldBe(6);
        product.Reservations.First().Status.ShouldBe(Domain.Enums.ReservationStatus.Confirmed);
    }

    [Fact]
    public void CancelReservation_WithPendingReservation_ShouldCancelReservationAndRestoreAvailableStock()
    {
        // Arrange
        var product = ProductFaker.CreateFaker(initialStock: 10).Generate();
        var invoiceId = Guid.NewGuid();
        product.ReserveStock(invoiceId, 4, TimeSpan.FromMinutes(30));
        product.AvailableStockQuantity.ShouldBe(6);

        // Act
        var result = product.CancelReservation(invoiceId);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        product.StockQuantity.ShouldBe(10);
        product.AvailableStockQuantity.ShouldBe(10);
        product.Reservations.First().Status.ShouldBe(Domain.Enums.ReservationStatus.Cancelled);
    }
}
