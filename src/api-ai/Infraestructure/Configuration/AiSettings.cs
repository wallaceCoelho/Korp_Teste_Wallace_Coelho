using Domain.Enums;

namespace Infraestructure.Configuration;

/// <summary>
/// Configurações do serviço de Inteligência Artificial.
/// </summary>
public sealed class AiSettings
{
    public const string SectionName = "AiSettings";

    /// <summary>
    /// Provedor ativo (Mock, OpenAI, Ollama, OpenAICompatible, AzureOpenAI).
    /// </summary>
    public AiProviderType Provider { get; set; } = AiProviderType.Mock;

    /// <summary>
    /// Identificador do modelo (ex: 'gpt-4o-mini', 'gpt-4o', 'llama3:8b', 'deepseek-chat', 'llama-3.3-70b-versatile').
    /// </summary>
    public string ModelId { get; set; } = "mock-ai-v1";

    /// <summary>
    /// Chave de API secreta (necessária para OpenAI, Groq, DeepSeek, etc.).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Endpoint base da API de IA (ex: 'https://api.openai.com/v1', 'https://api.groq.com/openai/v1', 'http://localhost:11434').
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Tempo limite de requisição em segundos.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Temperatura padrão (criatividade).
    /// </summary>
    public double DefaultTemperature { get; set; } = 0.7;

    /// <summary>
    /// Cota diária máxima de gerações de IA por cliente/IP (Padrão: 15 gerações por dia).
    /// </summary>
    public int DailyQuotaLimit { get; set; } = 15;
}
