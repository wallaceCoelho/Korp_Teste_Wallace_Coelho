namespace Application.Security;

/// <summary>
/// Informações sobre a cota diária de uso da IA do cliente.
/// </summary>
public sealed record DailyQuotaResult(
    bool IsAllowed,
    int TotalLimit,
    int UsedToday,
    int Remaining,
    DateTime ResetsAtUtc,
    string? ErrorMessage = null
);
