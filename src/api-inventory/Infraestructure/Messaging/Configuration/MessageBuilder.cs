using Domain.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Infraestructure.Messaging.Configuration;

public sealed class MessagingRegistry
{
    public Dictionary<string, ProducerOptions> Producers { get; } = [];
    public List<ConsumerOptions> Consumers { get; } = [];
}

public sealed class ProducerBuilder(MessagingRegistry registry)
{
    public ProducerBuilder Add<TConfig>() where TConfig : class
    {
        var configType = typeof(TConfig);
        var producerInterface = configType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IProducerConfig<>))
            ?? throw new InvalidOperationException($"{configType.Name} deve implementar IProducerConfig<TEvent>");

        var eventType = producerInterface.GetGenericArguments()[0];

        var options = new ProducerOptions();

        var configureMethod = configType.GetMethod(
            nameof(IProducerConfig<>.Configure),
            BindingFlags.Public | BindingFlags.Static);

        configureMethod?.Invoke(null, [options]);

        EventTypeRegistry.Register(eventType);

        registry.Producers[eventType.Name] = options;
        return this;
    }
}

public sealed class ConsumerBuilder(IServiceCollection services, MessagingRegistry registry)
{
    public ConsumerBuilder Add<THandler>() where THandler : class, IConsumerConfig
    {
        var handlerType = typeof(THandler);

        var options = new ConsumerOptions();
        THandler.Configure(options);

        var handlerInterfaces = handlerType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>));

        foreach (var @interface in handlerInterfaces)
        {
            services.AddScoped(@interface, handlerType);
            var eventType = @interface.GetGenericArguments()[0];
            EventTypeRegistry.Register(eventType);
        }

        registry.Consumers.Add(options);
        return this;
    }
}

public sealed class DeadLetterBuilder(IServiceCollection services)
{
    public DeadLetterBuilder Add<THandler>() where THandler : class
    {
        var handlerType = typeof(THandler);

        var dlInterfaces = handlerType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDeadLetterHandler<>));

        foreach (var @interface in dlInterfaces)
        {
            services.AddScoped(@interface, handlerType);
            var eventType = @interface.GetGenericArguments()[0];
            EventTypeRegistry.Register(eventType);
        }

        return this;
    }
}