using Application.Features.ProductDescription;
using Application.Guardrails;
using Application.Interfaces;
using Application.Security;
using Application.Services;
using Domain.Enums;
using Infraestructure.Configuration;
using Infraestructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infraestructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAiInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Configurações de IA
        var aiSection = configuration.GetSection(AiSettings.SectionName);
        services.Configure<AiSettings>(aiSection);

        var aiSettings = new AiSettings();
        aiSection.Bind(aiSettings);

        // 2. Provedor de IA (Mock ou Unificado)
        if (aiSettings.Provider == AiProviderType.Mock)
        {
            services.AddSingleton<IAiChatService, MockAiChatService>();
        }
        else
        {
            services.AddHttpClient<IAiChatService, UnifiedAiChatService>();
        }

        // 3. Camada de Segurança: Guardrails contra Prompt Injections e Jailbreak
        services.AddSingleton<IGuardrailService, ProductInputGuardrailService>();

        // 4. Camada de Segurança: Controle de Cota Diária em Memória
        services.AddSingleton<IDailyQuotaService, InMemoryDailyQuotaService>();

        // 5. Armazenamento em memória para tarefas e consultas de IA
        services.AddSingleton<IAiTaskStore, InMemoryAiTaskStore>();

        // 6. Handlers de recursos de IA (Strategy Pattern)
        services.AddScoped<IAiFeatureHandler, ProductDescriptionHandler>();

        // 7. Resolvedor Scoped de Recursos
        services.AddScoped<IAiFeatureResolver, AiFeatureResolver>();

        return services;
    }
}
