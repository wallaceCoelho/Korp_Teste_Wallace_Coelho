using System.Text.Json;

namespace Presentation.API.Extensions;

public static class SseExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task StreamSSEAsync<T>(
        this HttpContext httpContext,
        IAsyncEnumerable<T> stream,
        int timeoutSeconds = 30)
    {
        var response = httpContext.Response;
        response.Headers.Append("Content-Type", "text/event-stream");
        response.Headers.Append("Cache-Control", "no-cache");
        response.Headers.Append("Connection", "keep-alive");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted);
        cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        var ct = cts.Token;

        try
        {
            await foreach (var item in stream.WithCancellation(ct))
            {
                var jsonPayload = JsonSerializer.Serialize(item, JsonOptions);
                await response.WriteAsync($"data: {jsonPayload}\n\n", ct);
                await response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested || cts.IsCancellationRequested)
        {
            // Graceful completion on client disconnect or timeout
        }
    }
}
