using ApiGateway.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TesteMicroservicos.AuthServer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TesteMicroservicos.Clients";
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "SuperSecretKeyForJwtAuthenticationInApiGatewaySystem2026!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AuthenticatedUser", policy => policy.RequireAssertion(_ => true));

//builder.Services.AddAuthorizationBuilder()
//    .AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("FixedWindowPolicy", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new GatewayHealthResponse
(
    Status: "Healthy",
    Engine: "YARP Reverse Proxy",
    Timestamp: DateTime.UtcNow
)));

app.MapReverseProxy();

app.Run();

[JsonSerializable(typeof(GatewayHealthResponse))]
internal partial class GatewayJsonContext : JsonSerializerContext;

internal record GatewayHealthResponse(string Status, string Engine, DateTime Timestamp);
