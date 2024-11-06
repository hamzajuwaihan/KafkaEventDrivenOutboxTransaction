using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentSystem.Infra.Consumers;
using PaymentSystem.Infra.DB;
using PaymentSystem.Infra.Processors;
using PaymentSystem.Infra.Producers;
using PaymentSystem.Infra.Repositories;
using PaymentSystem.Infra.RepositoriesContracts;

namespace PaymentSystem.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ConsumerConfig consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = configuration["Kafka:Consumers:OrderCreatedConsumer:GroupId"] ?? "default-consumer-group",
            AutoOffsetReset = Enum.Parse<AutoOffsetReset>(configuration["Kafka:Consumer:AutoOffsetReset"] ?? "Earliest")
        };

        services.AddSingleton(consumerConfig);

        ProducerConfig producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            AllowAutoCreateTopics = bool.Parse(configuration["Kafka:Producer:AllowAutoCreateTopics"] ?? "true"),
            Acks = Acks.All
        };

        services.AddSingleton(producerConfig);

        services.AddSingleton<KafkaProducer>();

        services.AddDbContext<AppDbContext>(options =>
           options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        services.AddHostedService<OrderCreatedConsumer>();
        services.AddHostedService<OutboxProcessor>();

        services.AddScoped<IPaymentRepository, PaymentRepository>();

        return services;

    }

}
