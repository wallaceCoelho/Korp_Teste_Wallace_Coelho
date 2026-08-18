using System.Text.Json;
using Application.Interfaces;
using Application.Security;
using Domain.Enums;
using Domain.Models;
using Infraestructure.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Presentation.Endpoints;

public static class AiEndpoints
{
    public static void MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai").WithTags("Inteligência Artificial");

        group.MapGet("/quota", (
            HttpContext httpContext,
            IDailyQuotaService quotaService,
            IOptions<AiSettings> aiOptions) =>
        {
            var clientId = ResolveClientId(httpContext);
            var status = quotaService.GetQuotaStatus(clientId, aiOptions.Value.DailyQuotaLimit);
            return Results.Ok(status);
        })
        .WithName("GetAiQuotaStatus")
        .WithSummary("Consulta a cota diária restante de requisições de IA para o cliente")
        .Produces<DailyQuotaResult>(StatusCodes.Status200OK);

        group.MapPost("/requests", async (
            [FromBody] CreateAiRequestDto dto,
            IAiFeatureResolver resolver,
            IAiTaskStore taskStore,
            IDailyQuotaService quotaService,
            IOptions<AiSettings> aiOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var clientId = ResolveClientId(httpContext);
            var quota = quotaService.ConsumeQuota(clientId, aiOptions.Value.DailyQuotaLimit);

            ApplyQuotaHeaders(httpContext, quota);

            if (!quota.IsAllowed)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    detail: quota.ErrorMessage,
                    title: "Limite Diário de IA Atingido"
                );
            }

            var requestId = dto.RequestId ?? Guid.NewGuid();
            
            try
            {
                var handler = resolver.Resolve(dto.FeatureType);
                var payloadJson = dto.Payload is JsonElement element ? element.GetRawText() : JsonSerializer.Serialize(dto.Payload);

                var response = await handler.ExecuteAsync(requestId, payloadJson, cancellationToken);
                taskStore.Save(response);

                return Results.Created($"/api/ai/requests/{requestId}", response);
            }
            catch (NotSupportedException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Erro no processamento de IA"
                );
            }
        })
        .WithName("RequestAiFeature")
        .WithSummary("Solicita a execução de um recurso de IA")
        .WithDescription("Executa o recurso de IA especificado pelo enum (ex: ProductDescription) e retorna o conteúdo gerado.")
        .Produces<AiTaskResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status429TooManyRequests)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/requests/{id:guid}", (Guid id, IAiTaskStore taskStore) =>
        {
            var response = taskStore.Get(id);
            return response is not null 
                ? Results.Ok(response) 
                : Results.NotFound(new { error = $"Nenhuma requisição de IA encontrada com o ID '{id}'." });
        })
        .WithName("GetAiRequestById")
        .WithSummary("Consulta o resultado de uma requisição de IA pelo ID")
        .WithDescription("Recupera a resposta gerada e metadados de uma requisição de IA processada.")
        .Produces<AiTaskResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/features", (IAiFeatureResolver resolver, IAiChatService chatService) =>
        {
            var supported = resolver.GetSupportedFeatures().Select(f => new
            {
                FeatureId = (int)f,
                FeatureName = f.ToString(),
                Description = GetFeatureDescription(f)
            });

            return Results.Ok(new
            {
                ActiveProvider = chatService.ActiveProvider.ToString(),
                ActiveModel = chatService.ActiveModelId,
                SupportedFeatures = supported
            });
        })
        .WithName("ListAiFeatures")
        .WithSummary("Lista as capacidades de IA e o modelo ativo")
        .Produces(StatusCodes.Status200OK);

        group.MapPost("/product-description", async (
            [FromBody] ProductDescriptionPayload payload,
            IAiFeatureResolver resolver,
            IAiTaskStore taskStore,
            IDailyQuotaService quotaService,
            IOptions<AiSettings> aiOptions,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var clientId = ResolveClientId(httpContext);
            var quota = quotaService.ConsumeQuota(clientId, aiOptions.Value.DailyQuotaLimit);

            ApplyQuotaHeaders(httpContext, quota);

            if (!quota.IsAllowed)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status429TooManyRequests,
                    detail: quota.ErrorMessage,
                    title: "Limite Diário de IA Atingido"
                );
            }

            var requestId = Guid.NewGuid();
            var handler = resolver.Resolve(AiFeatureType.ProductDescription);
            var payloadJson = JsonSerializer.Serialize(payload);

            var response = await handler.ExecuteAsync(requestId, payloadJson, cancellationToken);
            taskStore.Save(response);

            return Results.Ok(response);
        })
        .WithName("GenerateProductDescription")
        .WithSummary("Gera uma descrição comercial e técnica para produto")
        .WithDescription("Recebe o nome do produto e palavras-chave para produzir uma descrição comercial de alto impacto.")
        .Produces<AiTaskResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status429TooManyRequests);
    }

    private static string ResolveClientId(HttpContext context)
    {
        var userId = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"user:{userId}";
        }

        if (context.Request.Headers.TryGetValue("CF-Connecting-IP", out var cfIp) && !string.IsNullOrWhiteSpace(cfIp))
        {
            return $"ip:{cfIp.ToString().Trim()}";
        }

        if (context.Request.Headers.TryGetValue("True-Client-IP", out var trueIp) && !string.IsNullOrWhiteSpace(trueIp))
        {
            return $"ip:{trueIp.ToString().Trim()}";
        }

        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) && !string.IsNullOrWhiteSpace(forwarded))
        {
            var firstIp = forwarded.ToString().Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(firstIp))
            {
                return $"ip:{firstIp}";
            }
        }

        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp) && !string.IsNullOrWhiteSpace(realIp))
        {
            return $"ip:{realIp.ToString().Trim()}";
        }

        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        return !string.IsNullOrWhiteSpace(remoteIp) ? $"ip:{remoteIp}" : "anonymous-client";
    }

    private static void ApplyQuotaHeaders(HttpContext context, DailyQuotaResult quota)
    {
        context.Response.Headers["X-Daily-Quota-Limit"] = quota.TotalLimit.ToString();
        context.Response.Headers["X-Daily-Quota-Remaining"] = quota.Remaining.ToString();
        context.Response.Headers["X-Daily-Quota-Reset"] = quota.ResetsAtUtc.ToString("o");
    }

    private static string GetFeatureDescription(AiFeatureType feature) => feature switch
    {
        AiFeatureType.ProductDescription => "Geração automatizada de descrição comercial e técnica de produtos para catálogo e e-commerce.",
        AiFeatureType.ProductTags => "Extração de tags e palavras-chave otimizadas para busca e SEO.",
        AiFeatureType.CategorySuggestion => "Sugestão inteligente da melhor categoria para enquadrar um produto.",
        AiFeatureType.InvoiceSummary => "Geração de resumo executivo e análise de notas fiscais.",
        _ => "Recurso de IA generativa."
    };
}

public sealed record CreateAiRequestDto(
    AiFeatureType FeatureType,
    object Payload,
    Guid? RequestId = null
);
