namespace Application.Guardrails;

/// <summary>
/// Contrato para o serviço de Guardrails (proteção contra prompt injection, jailbreaks e uso indevido como chat).
/// </summary>
public interface IGuardrailService
{
    /// <summary>
    /// Valida e sanitiza os dados de entrada de um produto para impedir prompt injections e conversações indevidas.
    /// </summary>
    GuardrailValidationResult ValidateProductInput(string productName, string? descriptionHint = null);

    /// <summary>
    /// Valida e limpa o texto gerado pelo modelo para assegurar que não vazou instruções de sistema ou saudações de chatbot.
    /// </summary>
    string CleanAndValidateOutput(string generatedText);
}
