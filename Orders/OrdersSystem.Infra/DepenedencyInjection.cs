using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrdersSystem.Infra.Producers;

namespace OrdersSystem.Infra;

public static class DependencyInjection
{

    public static IServiceCollection AddKafkaConsumer(this IServiceCollection services, IConfiguration configuration)
    {
        ConsumerConfig consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = configuration["Kafka:Consumers:PaymentProcessConsumer:GroupId"] ?? "default-consumer-group",
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(configuration["Kafka:Consumer:AutoOffsetReset"] ?? "Earliest")
        };

        services.AddSingleton(consumerConfig);
        
        return services;
    }

    public static IServiceCollection AddKafkaProducer(this IServiceCollection services, IConfiguration configuration)
    {
        ProducerConfig producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            AllowAutoCreateTopics = bool.Parse(configuration["Kafka:Producer:AllowAutoCreateTopics"] ?? "true"),
            Acks = Acks.All
        };

        services.AddSingleton(producerConfig);

        services.AddSingleton<KafkaProducer>();

        return services;
    }

}
