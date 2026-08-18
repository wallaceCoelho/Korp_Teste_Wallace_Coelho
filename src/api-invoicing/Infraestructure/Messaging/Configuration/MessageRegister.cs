using Infraestructure.Background;
using Microsoft.Extensions.DependencyInjection;

namespace Infraestructure.Messaging.Configuration;

internal static class MessageRegister
{
    private static readonly MessagingRegistry Registry = new();

    public static IServiceCollection AddProducers(this IServiceCollection services, Action<ProducerBuilder> configure)
    {
        services.AddSingleton(Registry);
        var builder = new ProducerBuilder(Registry);
        configure(builder);
        return services;
    }

    public static IServiceCollection AddConsumers(this IServiceCollection services, Action<ConsumerBuilder> configure)
    {
        services.AddSingleton(Registry);
        var builder = new ConsumerBuilder(services, Registry);
        configure(builder);
        return services;
    }

    public static IServiceCollection AddDeadLetterHandlers(this IServiceCollection services, Action<DeadLetterBuilder> configure)
    {
        services.AddSingleton(Registry);
        var builder = new DeadLetterBuilder(services);
        configure(builder);
        return services;
    }

    public static IServiceCollection AddWorkerMessages(this IServiceCollection services)
    {
        services.AddHostedService<OutboxParallelProcessorWorker>();
        services.AddHostedService<InboxParallelProcessorWorker>();
        services.AddHostedService<DeadLetterCompensationWorker>();
        services.AddHostedService<RabbitMqIngestionWorker>();
        return services;
    }
}
