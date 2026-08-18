using Domain.Enums;
using FluentAssertions;
using Infraestructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AiService.Tests;

public class MockAiChatServiceTests
{
    private readonly MockAiChatService _sut = new(NullLogger<MockAiChatService>.Instance);

    [Fact]
    public async Task GenerateTextAsync_ShouldReturnContextualDescription()
    {
        // Arrange
        var prompt = "Gere uma descrição para o produto:\n- Nome: Placa de Vídeo RTX 4080";

        // Act
        var result = await _sut.GenerateTextAsync(prompt);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().Contain("Placa de Vídeo RTX 4080");
        _sut.ActiveProvider.Should().Be(AiProviderType.Mock);
    }
}
