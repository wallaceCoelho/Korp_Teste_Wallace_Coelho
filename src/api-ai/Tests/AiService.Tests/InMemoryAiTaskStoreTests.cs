using Application.Services;
using Domain.Enums;
using Domain.Models;
using FluentAssertions;
using Xunit;

namespace AiService.Tests;

public class InMemoryAiTaskStoreTests
{
    [Fact]
    public void SaveAndGet_ShouldStoreAndRetrieveTask()
    {
        // Arrange
        var store = new InMemoryAiTaskStore();
        var requestId = Guid.NewGuid();
        var response = AiTaskResponse.Success(
            requestId: requestId,
            featureType: AiFeatureType.ProductDescription,
            content: "Descrição gerada por teste",
            modelUsed: "mock-ai-v1",
            providerUsed: AiProviderType.Mock,
            duration: TimeSpan.FromMilliseconds(150)
        );

        // Act
        store.Save(response);
        var retrieved = store.Get(requestId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.RequestId.Should().Be(requestId);
        retrieved.GeneratedContent.Should().Be("Descrição gerada por teste");
        retrieved.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Get_NonExistentId_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryAiTaskStore();

        // Act
        var result = store.Get(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetAll_ShouldReturnSavedTasksOrderedByDate()
    {
        // Arrange
        var store = new InMemoryAiTaskStore();
        var resp1 = AiTaskResponse.Success(Guid.NewGuid(), AiFeatureType.ProductDescription, "Item 1", "m", AiProviderType.Mock, TimeSpan.Zero);
        var resp2 = AiTaskResponse.Success(Guid.NewGuid(), AiFeatureType.ProductDescription, "Item 2", "m", AiProviderType.Mock, TimeSpan.Zero);

        store.Save(resp1);
        store.Save(resp2);

        // Act
        var all = store.GetAll(10);

        // Assert
        all.Should().HaveCount(2);
    }
}
