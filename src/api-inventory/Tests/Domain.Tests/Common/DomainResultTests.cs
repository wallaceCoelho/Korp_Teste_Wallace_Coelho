using Domain.Common;
using Shouldly;
using Xunit;

namespace Domain.Tests.Common;

public class DomainResultTests
{
    [Fact]
    public void DomainResult_Success_ShouldReturnIsSuccessTrueAndNullError()
    {
        // Act
        var result = DomainResult.Success();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void DomainResult_Failure_ShouldReturnIsSuccessFalseAndErrorMessage()
    {
        // Arrange
        const string errorMessage = "Erro de validação";

        // Act
        var result = DomainResult.Failure(errorMessage);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(errorMessage);
    }

    [Fact]
    public void DomainResult_ImplicitConversionFromString_ShouldCreateFailure()
    {
        // Arrange
        const string errorMessage = "Erro implícito";

        // Act
        DomainResult result = errorMessage;

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(errorMessage);
    }

    [Fact]
    public void DomainResultTyped_Success_ShouldReturnIsSuccessTrueAndValue()
    {
        // Arrange
        const string expectedValue = "Valor Sucesso";

        // Act
        var result = DomainResult<string>.Success(expectedValue);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expectedValue);
        result.Error.ShouldBeNull();
    }

    [Fact]
    public void DomainResultTyped_Failure_ShouldReturnIsSuccessFalseAndError()
    {
        // Arrange
        const string errorMessage = "Falha no valor";

        // Act
        var result = DomainResult<string>.Failure(errorMessage);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Error.ShouldBe(errorMessage);
    }

    [Fact]
    public void DomainResultTyped_ImplicitConversions_ShouldBehaveCorrectly()
    {
        // Act & Assert (Implicit error)
        DomainResult<int> failureResult = "Erro de conversão";
        failureResult.IsSuccess.ShouldBeFalse();
        failureResult.Error.ShouldBe("Erro de conversão");
        failureResult.Value.ShouldBe(default);

        // Act & Assert (Implicit value)
        DomainResult<int> successResult = 42;
        successResult.IsSuccess.ShouldBeTrue();
        successResult.Value.ShouldBe(42);
        successResult.Error.ShouldBeNull();
    }
}
