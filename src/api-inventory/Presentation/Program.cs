using Application;
using Infraestructure;
using Infraestructure.Persistence;
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

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

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
    options.Title = "Inventory API - Documentação Scalar";
    options.Theme = ScalarTheme.Purple;
    options.DefaultHttpClient = new(ScalarTarget.Http, ScalarClient.Http11);
});

app.MapGet("/health", () => Results.Ok(new { Service = "Inventory API", Status = "Healthy" }))
   .WithName("InventoryHealthCheck")
   .WithSummary("Verifica a saúde do serviço de Inventário")
   .Produces(StatusCodes.Status200OK);

app.MapGet("/", () => Results.Redirect("/scalar/v1"))
   .ExcludeFromDescription();

app.MapProductsEndpoints();
app.MapCategoriesEndpoints();

try
{
    await app.Services.ApplyMigrationsAsync();
    await app.Services.SeedInventoryDataAsync();
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Error during migration and data seeding initialization.");
}

app.Run();