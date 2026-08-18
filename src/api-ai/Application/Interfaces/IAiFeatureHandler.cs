using Domain.Enums;
using Domain.Models;

namespace Application.Interfaces;

/// <summary>
/// Contrato para manipuladores de recursos de IA específicos (Strategy Pattern).
/// </summary>
public interface IAiFeatureHandler
{
    AiFeatureType FeatureType { get; }

    Task<AiTaskResponse> ExecuteAsync(Guid requestId, string inputPayloadJson, CancellationToken cancellationToken = default);
}
