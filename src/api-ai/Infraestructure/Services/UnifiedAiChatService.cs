using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Interfaces;
using Domain.Enums;
using Infraestructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infraestructure.Services;

/// <summary>
/// Serviço unificado de IA compatível com protocolo OpenAI (OpenAI, Groq, DeepSeek, Ollama, vLLM, LM Studio).
/// </summary>
public sealed class UnifiedAiChatService : IAiChatService
{
    private readonly HttpClient _httpClient;
    private readonly AiSettings _settings;
    private readonly ILogger<UnifiedAiChatService> _logger;

    public AiProviderType ActiveProvider => _settings.Provider;
    public string ActiveModelId => _settings.ModelId;

    public UnifiedAiChatService(
        HttpClient httpClient, 
        IOptions<AiSettings> options, 
        ILogger<UnifiedAiChatService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;

        ConfigureClient();
    }

    private void ConfigureClient()
    {
        var endpoint = _settings.Endpoint?.Trim();
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = _settings.Provider switch
            {
                AiProviderType.Ollama => "http://localhost:11434/v1",
                _ => "https://api.openai.com/v1"
            };
        }

        if (!endpoint.EndsWith('/')) endpoint += "/";
        _httpClient.BaseAddress = new Uri(endpoint);
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(10, _settings.TimeoutSeconds));

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey.Trim());
        }
    }

    public async Task<string> GenerateTextAsync(
        string prompt, 
        string? systemPrompt = null, 
        double temperature = 0.7, 
        int maxTokens = 1000, 
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessagePayload>();

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new ChatMessagePayload("system", systemPrompt));
        }

        messages.Add(new ChatMessagePayload("user", prompt));

        var requestPayload = new ChatCompletionRequestPayload(
            Model: _settings.ModelId,
            Messages: messages,
            Temperature: temperature > 0 ? temperature : _settings.DefaultTemperature,
            MaxTokens: maxTokens > 0 ? maxTokens : 1000
        );

        var jsonContent = JsonSerializer.Serialize(requestPayload);
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
        };

        _logger.LogInformation("[UnifiedAi] Enviando requisição para provedor '{Provider}' com modelo '{Model}'...", 
            _settings.Provider, _settings.ModelId);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("[UnifiedAi] Erro da API ({StatusCode}): {Body}", response.StatusCode, errorBody);
            throw new HttpRequestException($"Provedor de IA retornou status {response.StatusCode}: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsedResponse = JsonSerializer.Deserialize<ChatCompletionResponsePayload>(responseBody, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var reply = parsedResponse?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(reply))
        {
            throw new InvalidOperationException("A API de IA retornou uma resposta vazia.");
        }

        return reply.Trim();
    }

    private sealed record ChatMessagePayload(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content
    );

    private sealed record ChatCompletionRequestPayload(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] List<ChatMessagePayload> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens
    );

    private sealed record ChatCompletionChoice(
        [property: JsonPropertyName("message")] ChatMessagePayload Message
    );

    private sealed record ChatCompletionResponsePayload(
        [property: JsonPropertyName("choices")] List<ChatCompletionChoice>? Choices
    );
}
