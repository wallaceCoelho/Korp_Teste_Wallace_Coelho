using Domain.Enums;

namespace Domain.Models;

/// <summary>
/// Parâmetros de entrada para o recurso de geração de descrição de produto.
/// </summary>
public sealed record ProductDescriptionPayload(
    string ProductName,
    string? CategoryName = null,
    string? DescriptionHint = null,
    AiToneType Tone = AiToneType.Commercial,
    string Language = "pt-BR",
    int MaxCharacters = 500
);
