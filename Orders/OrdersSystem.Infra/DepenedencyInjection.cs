using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrdersSystem.Infra.Consumers;
using OrdersSystem.Infra.DB;
using OrdersSystem.Infra.Processors;
using OrdersSystem.Infra.Producers;
using OrdersSystem.Infra.Repositories;
using OrdersSystem.Infra.RepositoriesContracts;

namespace OrdersSystem.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ConsumerConfig consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = configuration["Kafka:Consumers:PaymentProcessConsumer:GroupId"] ?? "default-consumer-group",
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

        services.AddScoped<IOrderRepository, OrdersRepository>();


        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddHostedService<PaymentProcessTopicConsumer>();

        services.AddHostedService<OutboxProcessor>();

        return services;
    }

}
