using Application.Interfaces;
using Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Infraestructure.Services;

/// <summary>
/// Provedor simulado de IA que gera respostas contextuais realistas sem necessidade de conexão externa ou custos.
/// </summary>
public sealed class MockAiChatService(ILogger<MockAiChatService> logger) : IAiChatService
{
    public AiProviderType ActiveProvider => AiProviderType.Mock;
    public string ActiveModelId => "mock-ai-v1 (Simulado Local)";

    public async Task<string> GenerateTextAsync(
        string prompt, 
        string? systemPrompt = null, 
        double temperature = 0.7, 
        int maxTokens = 1000, 
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[MockAi] Simulando geração de texto para o prompt: {PromptSnippet}...", 
            prompt.Length > 60 ? prompt[..60] : prompt);

        await Task.Delay(200, cancellationToken);

        var productName = "Produto";
        var lines = prompt.Split('\n');
        foreach (var line in lines)
        {
            if (line.Contains("Nome:", StringComparison.OrdinalIgnoreCase))
            {
                productName = line.Replace("Nome:", "", StringComparison.OrdinalIgnoreCase).Replace("-", "").Trim();
                break;
            }
        }

        return $"""
            O {productName} foi desenvolvido para oferecer máxima performance, confiabilidade e excelência em sua categoria. Com acabamento refinado e materiais de alta durabilidade, proporciona uma experiência superior no uso diário, combinando design moderno, eficiência energética e padrão de qualidade rigoroso para atender às necessidades mais exigentes.
            """;
    }
}
