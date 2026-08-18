namespace Application.Security;

/// <summary>
/// Contrato para rastreamento e controle da cota diária de consumo da IA.
/// </summary>
public interface IDailyQuotaService
{
    /// <summary>
    /// Verifica e consome 1 unidade da cota diária do cliente.
    /// </summary>
    DailyQuotaResult ConsumeQuota(string clientIdentifier, int maxDailyLimit = 15);

    /// <summary>
    /// Consulta o estado atual da cota diária sem incrementar o contador.
    /// </summary>
    DailyQuotaResult GetQuotaStatus(string clientIdentifier, int maxDailyLimit = 15);
}
