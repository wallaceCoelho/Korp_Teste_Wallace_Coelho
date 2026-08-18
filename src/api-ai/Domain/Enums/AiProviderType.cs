namespace Domain.Enums;

/// <summary>
/// Provedores de Inteligência Artificial e Modelos suportados pelo microsserviço.
/// </summary>
public enum AiProviderType
{
    /// <summary>
    /// Provedor simulado local (offline, não consome créditos/chaves de API).
    /// </summary>
    Mock = 0,

    /// <summary>
    /// OpenAI (GPT-4o, GPT-4o-mini, GPT-3.5-turbo, etc.).
    /// </summary>
    OpenAI = 1,

    /// <summary>
    /// Ollama local ou remoto (Llama3, Mistral, Qwen, DeepSeek, Phi3).
    /// </summary>
    Ollama = 2,

    /// <summary>
    /// Provedores compatíveis com protocolo OpenAI (Groq, DeepSeek API, Together AI, vLLM, LM Studio).
    /// </summary>
    OpenAICompatible = 3,

    /// <summary>
    /// Azure OpenAI Service.
    /// </summary>
    AzureOpenAI = 4
}
