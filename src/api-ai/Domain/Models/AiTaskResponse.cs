using Domain.Enums;

namespace Domain.Models;

/// <summary>
/// Representa a resposta ou resultado do processamento de um recurso de IA.
/// </summary>
public sealed record AiTaskResponse(
    Guid RequestId,
    AiFeatureType FeatureType,
    string GeneratedContent,
    string ModelUsed,
    AiProviderType ProviderUsed,
    TimeSpan ExecutionDuration,
    DateTime CompletedAt,
    bool IsSuccess,
    string? ErrorMessage = null
)
{
    public static AiTaskResponse Success(
        Guid requestId,
        AiFeatureType featureType,
        string content,
        string modelUsed,
        AiProviderType providerUsed,
        TimeSpan duration) =>
        new(
            RequestId: requestId,
            FeatureType: featureType,
            GeneratedContent: content,
            ModelUsed: modelUsed,
            ProviderUsed: providerUsed,
            ExecutionDuration: duration,
            CompletedAt: DateTime.UtcNow,
            IsSuccess: true
        );

    public static AiTaskResponse Failure(
        Guid requestId,
        AiFeatureType featureType,
        string errorMessage,
        string modelUsed,
        AiProviderType providerUsed) =>
        new(
            RequestId: requestId,
            FeatureType: featureType,
            GeneratedContent: string.Empty,
            ModelUsed: modelUsed,
            ProviderUsed: providerUsed,
            ExecutionDuration: TimeSpan.Zero,
            CompletedAt: DateTime.UtcNow,
            IsSuccess: false,
            ErrorMessage: errorMessage
        );
}
