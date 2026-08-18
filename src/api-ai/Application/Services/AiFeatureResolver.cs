using Application.Interfaces;
using Domain.Enums;

namespace Application.Services;

public sealed class AiFeatureResolver(IEnumerable<IAiFeatureHandler> handlers) : IAiFeatureResolver
{
    private readonly Dictionary<AiFeatureType, IAiFeatureHandler> _handlers = handlers.ToDictionary(h => h.FeatureType);

    public IAiFeatureHandler Resolve(AiFeatureType featureType)
    {
        if (_handlers.TryGetValue(featureType, out var handler))
        {
            return handler;
        }

        throw new NotSupportedException($"O recurso de IA '{featureType}' não possui um manipulador registrado.");
    }

    public IEnumerable<AiFeatureType> GetSupportedFeatures() => _handlers.Keys;
}
