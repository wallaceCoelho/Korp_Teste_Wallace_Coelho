using Domain.Enums;

namespace Application.Interfaces;

/// <summary>
/// Resolvedor scoped de manipuladores de recursos de IA com base no enum do recurso.
/// </summary>
public interface IAiFeatureResolver
{
    IAiFeatureHandler Resolve(AiFeatureType featureType);
    IEnumerable<AiFeatureType> GetSupportedFeatures();
}
