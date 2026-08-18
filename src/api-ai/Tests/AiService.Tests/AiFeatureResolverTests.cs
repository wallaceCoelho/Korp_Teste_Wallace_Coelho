using Application.Features.ProductDescription;
using Application.Interfaces;
using Application.Services;
using Domain.Enums;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AiService.Tests;

public class AiFeatureResolverTests
{
    [Fact]
    public void Resolve_RegisteredFeature_ShouldReturnHandler()
    {
        // Arrange
        var mockHandler = Substitute.For<IAiFeatureHandler>();
        mockHandler.FeatureType.Returns(AiFeatureType.ProductDescription);

        var resolver = new AiFeatureResolver(new[] { mockHandler });

        // Act
        var result = resolver.Resolve(AiFeatureType.ProductDescription);

        // Assert
        result.Should().NotBeNull();
        result.FeatureType.Should().Be(AiFeatureType.ProductDescription);
    }

    [Fact]
    public void Resolve_UnregisteredFeature_ShouldThrowNotSupportedException()
    {
        // Arrange
        var resolver = new AiFeatureResolver(Enumerable.Empty<IAiFeatureHandler>());

        // Act
        var act = () => resolver.Resolve(AiFeatureType.ProductTags);

        // Assert
        act.Should().Throw<NotSupportedException>()
           .WithMessage("*ProductTags*");
    }

    [Fact]
    public void GetSupportedFeatures_ShouldReturnAllRegisteredTypes()
    {
        // Arrange
        var handler1 = Substitute.For<IAiFeatureHandler>();
        handler1.FeatureType.Returns(AiFeatureType.ProductDescription);

        var handler2 = Substitute.For<IAiFeatureHandler>();
        handler2.FeatureType.Returns(AiFeatureType.ProductTags);

        var resolver = new AiFeatureResolver(new[] { handler1, handler2 });

        // Act
        var features = resolver.GetSupportedFeatures();

        // Assert
        features.Should().Contain(new[] { AiFeatureType.ProductDescription, AiFeatureType.ProductTags });
    }
}
