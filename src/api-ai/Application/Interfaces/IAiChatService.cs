using Domain.Enums;

namespace Application.Interfaces;

/// <summary>
/// Contrato de serviço unificado para comunicação com modelos de IA (LLMs).
/// </summary>
public interface IAiChatService
{
    AiProviderType ActiveProvider { get; }
    string ActiveModelId { get; }

    Task<string> GenerateTextAsync(
        string prompt, 
        string? systemPrompt = null, 
        double temperature = 0.7, 
        int maxTokens = 1000, 
        CancellationToken cancellationToken = default);
}
