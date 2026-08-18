using Domain.Enums;

namespace Domain.Models;

/// <summary>
/// Representa uma requisição genérica de IA processada pelo microsserviço.
/// </summary>
public sealed record AiTaskRequest(
    Guid RequestId,
    AiFeatureType FeatureType,
    string InputPrompt,
    string? ContextData = null,
    DateTime RequestedAt = default
)
{
    public DateTime RequestedAt { get; init; } = RequestedAt == default ? DateTime.UtcNow : RequestedAt;
}
