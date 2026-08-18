using Application.Common.Interfaces;
using Application.Features.Products.Integration.Consumers;
using Application.Features.Products.Integration.Consumers.DeadLetter;
using Application.Features.Products.Integration.Producers;
using Infraestructure.Messaging.Configuration;
using Infraestructure.Messaging.Interfaces;
using Infraestructure.Messaging.RabbitMQ;
using Infraestructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Infraestructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraestructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<InventoryDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(InventoryDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });
        });

        services.AddScoped<IUnitWork, UnitWork>();

        services.AddMessageBroker(configuration);

        return services;
    }

    public static IServiceCollection AddMessageBroker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(sp =>
        {
            var factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMQ:Host"] ?? "localhost",
                Port = int.TryParse(configuration["RabbitMQ:Port"], out var port) ? port : 5672,
                UserName = configuration["RabbitMQ:Username"] ?? "guest",
                Password = configuration["RabbitMQ:Password"] ?? "guest"
            };
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        services.AddSingleton<IRabbitMqProducer, RabbitMqProducer>();

        services.AddProducers(builder =>
        {
            builder.Add<InventoryReservedProducerHandler>();
            builder.Add<InventoryReservationFailedProducerHandler>();
            builder.Add<InventoryConfirmedProducerHandler>();
            builder.Add<InventoryConfirmationFailedProducerHandler>();
            builder.Add<InventoryReservedCanceledProducerHandler>();
        });

        services.AddConsumers(builder =>
        {
            builder.Add<InventoryReserveStockConsumerHandler>();
            builder.Add<InventoryConfirmStockConsumerHandler>();
            builder.Add<InventoryReleaseStockConsumerHandler>();
        });

        services.AddDeadLetterHandlers(builder =>
        {
            builder.Add<InvoiceCreatedDeadLetterHandler>();
            builder.Add<InvoicePrintConfirmedDeadLetterHandler>();
        });

        services.AddWorkerMessages();

        return services;
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        if ((await context.Database.GetPendingMigrationsAsync()).Any())
            await context.Database.MigrateAsync();
    }
}
