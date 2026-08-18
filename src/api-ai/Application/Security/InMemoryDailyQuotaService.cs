using System.Collections.Concurrent;

namespace Application.Security;

public sealed class InMemoryDailyQuotaService : IDailyQuotaService
{
    private sealed record ClientQuotaEntry(int Count, DateOnly Date);

    private readonly ConcurrentDictionary<string, ClientQuotaEntry> _quotaEntries = new();

    public DailyQuotaResult ConsumeQuota(string clientIdentifier, int maxDailyLimit = 15)
    {
        var clientId = NormalizeClientId(clientIdentifier);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var resetsAtUtc = DateTime.UtcNow.Date.AddDays(1);

        var updated = _quotaEntries.AddOrUpdate(
            clientId,
            _ => new ClientQuotaEntry(1, today),
            (_, existing) =>
            {
                if (existing.Date != today)
                {
                    // Novo dia: reseta o contador para 1
                    return new ClientQuotaEntry(1, today);
                }

                // Incrementa o contador
                return existing with { Count = existing.Count + 1 };
            }
        );

        if (updated.Count > maxDailyLimit)
        {
            return new DailyQuotaResult(
                IsAllowed: false,
                TotalLimit: maxDailyLimit,
                UsedToday: updated.Count,
                Remaining: 0,
                ResetsAtUtc: resetsAtUtc,
                ErrorMessage: $"Você atingiu o limite diário de {maxDailyLimit} gerações com Inteligência Artificial. Sua cota será renovada à meia-noite (UTC)."
            );
        }

        return new DailyQuotaResult(
            IsAllowed: true,
            TotalLimit: maxDailyLimit,
            UsedToday: updated.Count,
            Remaining: Math.Max(0, maxDailyLimit - updated.Count),
            ResetsAtUtc: resetsAtUtc
        );
    }

    public DailyQuotaResult GetQuotaStatus(string clientIdentifier, int maxDailyLimit = 15)
    {
        var clientId = NormalizeClientId(clientIdentifier);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var resetsAtUtc = DateTime.UtcNow.Date.AddDays(1);

        if (_quotaEntries.TryGetValue(clientId, out var entry) && entry.Date == today)
        {
            var remaining = Math.Max(0, maxDailyLimit - entry.Count);
            var isAllowed = entry.Count < maxDailyLimit;
            return new DailyQuotaResult(
                IsAllowed: isAllowed,
                TotalLimit: maxDailyLimit,
                UsedToday: entry.Count,
                Remaining: remaining,
                ResetsAtUtc: resetsAtUtc,
                ErrorMessage: isAllowed ? null : $"Limite diário de {maxDailyLimit} gerações atingido."
            );
        }

        return new DailyQuotaResult(
            IsAllowed: true,
            TotalLimit: maxDailyLimit,
            UsedToday: 0,
            Remaining: maxDailyLimit,
            ResetsAtUtc: resetsAtUtc
        );
    }

    private static string NormalizeClientId(string? clientIdentifier)
    {
        return string.IsNullOrWhiteSpace(clientIdentifier) ? "anonymous-client" : clientIdentifier.Trim().ToLowerInvariant();
    }
}
