namespace Application.Guardrails;

/// <summary>
/// Resultado da análise de segurança e integridade de entrada (Guardrails).
/// </summary>
public sealed record GuardrailValidationResult(
    bool IsValid,
    string? ViolationReason = null,
    string? SanitizedProductName = null,
    string? SanitizedDescriptionHint = null
)
{
    public static GuardrailValidationResult Success(string sanitizedName, string? sanitizedHint) =>
        new(true, null, sanitizedName, sanitizedHint);

    public static GuardrailValidationResult Blocked(string reason) =>
        new(false, reason, null, null);
}
