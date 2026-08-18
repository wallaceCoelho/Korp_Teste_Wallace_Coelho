using Application;
using Infraestructure;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Presentation.API.Endpoints;
using Presentation.Middlewares;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddInfraestructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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
               .AddNpgsql()
               .AddSource("RabbitMQ.Client");
    })
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddNpgsqlInstrumentation();
    })
    .UseOtlpExporter();

var app = builder.Build();

app.UseCors();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Invoicing API - Documentação Scalar";
    options.Theme = ScalarTheme.Purple;
    options.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.Http11);
});

app.MapGet("/health", () => Results.Ok(new { Service = "Invoicing API", Status = "Healthy" }))
   .WithName("InvoicingHealthCheck")
   .WithSummary("Verifica a saúde do serviço de Faturamento")
   .Produces(StatusCodes.Status200OK);

app.MapGet("/", () => Results.Redirect("/scalar/v1"))
   .ExcludeFromDescription();

app.MapInvoicesEndpoints();

try
{
    app.Services.ApplyMigrationsAsync().GetAwaiter().GetResult();
}
catch { }

app.Run();
