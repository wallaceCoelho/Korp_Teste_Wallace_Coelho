using Infraestructure;
using Presentation.Endpoints;
using Scalar.AspNetCore;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddAiInfrastructure(builder.Configuration);

builder.Services.AddOpenApi();

builder.Logging.AddOpenTelemetry(logging =>
{
    logging.IncludeFormattedMessage = true;
    logging.IncludeScopes = true;
});

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddSource("Microsoft.Extensions.AI")
               .AddSource("Microsoft.SemanticKernel*");
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddMeter("Microsoft.Extensions.AI")
               .AddMeter("Microsoft.SemanticKernel*");
    })
    .UseOtlpExporter();

var app = builder.Build();

app.UseCors("AllowAll");

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.WithTitle("AI Microservice API - Documentação Interativa")
            .WithTheme(ScalarTheme.Moon)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

app.MapGet("/health", () => Results.Ok(new { Service = "IA Integration API", Status = "Healthy" }))
   .WithName("IAHealthCheck")
   .WithSummary("Verifica a saúde do serviço de IA")
   .Produces(StatusCodes.Status200OK);

app.MapGet("/", () => Results.Redirect("/scalar/v1"))
   .ExcludeFromDescription();

app.MapAiEndpoints();

app.Run();
