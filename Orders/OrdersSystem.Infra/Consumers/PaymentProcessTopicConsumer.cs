using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrdersSystem.Domain.Entities;
using OrdersSystem.Domain.Messages;
using OrdersSystem.Infra.RepositoriesContracts;


namespace OrdersSystem.Infra.Consumers;

public class PaymentProcessTopicConsumer : BackgroundService
{
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly ILogger<PaymentProcessTopicConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public PaymentProcessTopicConsumer(IConfiguration configuration, ILogger<PaymentProcessTopicConsumer> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        string? bootstrapServers = configuration["Kafka:BootstrapServers"];
        string? groupId = configuration["Kafka:Consumers:PaymentProcessConsumer:GroupId"];
        string? autoOffsetResetString = configuration["Kafka:Consumers:PaymentProcessConsumer:AutoOffsetReset"];
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
            EnableAutoCommit = false,
            SessionTimeoutMs = 10000,
            ReconnectBackoffMs = 1000,
            ReconnectBackoffMaxMs = 10000,
            SocketKeepaliveEnable = true
        };

        _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        _consumer.Subscribe("PaymentProcessed");
        _logger.LogInformation("Subscribed to topic: PaymentProcessed");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(() => _logger.LogInformation("Cancellation requested, stopping the consumer..."));

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

                _logger.LogInformation($"Received PaymentProcessed event with message: {consumeResult.Message.Value}");

                PaymentProcessedMessage? orderMessage = JsonSerializer.Deserialize<PaymentProcessedMessage>(consumeResult.Message.Value);
                if (orderMessage != null)
                {
                    using (IServiceScope scope = _serviceProvider.CreateScope())
                    {
                        var ordersRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>(); // Use interface here

                        Order result = await ordersRepository.UpdateOrder(Guid.Parse(orderMessage.OrderId), orderMessage.Status);

                        _logger.LogInformation($"Order has been updated. New order with ID {result.Id}, status is: {result.Status}");
                    }
                }

                _consumer.Commit(consumeResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Processing error: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }


    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Kafka consumer...");
        return base.StopAsync(cancellationToken);
    }
}
