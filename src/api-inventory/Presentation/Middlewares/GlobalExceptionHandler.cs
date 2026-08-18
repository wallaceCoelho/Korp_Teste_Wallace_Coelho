using Application.Common.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization.Metadata;

namespace Presentation.Middlewares;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IOptions<JsonOptions> jsonOptions) : IExceptionHandler
{
    private readonly JsonOptions _jsonOptions = jsonOptions.Value;
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Ocorreu uma exceção não tratada durante a execução da requisição: {Message}", exception.Message);

        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Erro Interno no Servidor",
            Detail = "Ocorreu um erro inesperado. Por favor, tente novamente mais tarde.",
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails.Title,
            jsonTypeInfo: (JsonTypeInfo<ApiError>)_jsonOptions.SerializerOptions.GetTypeInfo(typeof(ApiError)),
            cancellationToken: cancellationToken);

        return true;
    }
}
