using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace ApiGateway.Middlewares;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Ocorreu um erro não tratado no API Gateway: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erro de Infraestrutura do API Gateway",
            Detail = "O gateway encontrou um erro inesperado ao processar o proxy da requisição.",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            ApiGatewayJsonSerializerContext.Default.ProblemDetails,
            cancellationToken: cancellationToken);

        return true;
    }
}

[JsonSerializable(typeof(ProblemDetails))]
public partial class ApiGatewayJsonSerializerContext : JsonSerializerContext;
