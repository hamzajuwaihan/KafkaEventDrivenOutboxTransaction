using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentSystem.Infra.Messages;
using PaymentSystem.Infra.Repositories;
using Microsoft.Extensions.Configuration;

namespace PaymentSystem.Infra.Consumers;
public class OrderCreatedConsumer : BackgroundService
{
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public OrderCreatedConsumer(IConfiguration configuration, ILogger<OrderCreatedConsumer> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        string? bootstrapServers = configuration["Kafka:BootstrapServers"];
        string? groupId = configuration["Kafka:Consumers:OrderCreatedConsumer:GroupId"];

        string? autoOffsetResetString = configuration["Kafka:Consumers:OrderCreatedConsumer:AutoOffsetReset"];
        AutoOffsetReset autoOffsetReset;

        if (!Enum.TryParse(autoOffsetResetString, true, out autoOffsetReset))
        {
            autoOffsetReset = AutoOffsetReset.Earliest;
            _logger.LogWarning("Invalid AutoOffsetReset value in configuration. Defaulting to 'Earliest'.");
        }

        ConsumerConfig consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = autoOffsetReset,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        _consumer.Subscribe("OrderCreated");
        _logger.LogInformation("Subscribed to topic: OrderCreated");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                ConsumeResult<Ignore, string> consumeResult = _consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value == null)
                {
                    _logger.LogWarning("Received a null or empty message.");
                    continue;
                }

                _logger.LogInformation($"Received OrderCreated event with message: {consumeResult.Message.Value}");

                OrderCreatedMessage? orderMessage = JsonSerializer.Deserialize<OrderCreatedMessage>(consumeResult.Message.Value);
                if (orderMessage != null)
                {
                    using IServiceScope scope = _serviceProvider.CreateScope();
                    PaymentRepository paymentRepository = scope.ServiceProvider.GetRequiredService<PaymentRepository>();

                    Domain.Entities.Payment result = await paymentRepository.CreatePayment(orderMessage.Id, orderMessage.Amount);

                    _logger.LogInformation($"Order has been updated. New order with ID {result.Id}, status is: {result.Status}");
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError($"Error occurred: {ex.Error.Reason}");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _consumer.Close();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Kafka consumer...");
        _consumer.Close();
        _consumer.Dispose();
        await base.StopAsync(cancellationToken);
    }
}
