using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentSystem.Infra.Repositories;
using Microsoft.Extensions.Configuration;
using PaymentSystem.Domain.Messages;
using PaymentSystem.Domain.Entities;
using PaymentSystem.Infra.RepositoriesContracts;

namespace PaymentSystem.Infra.Consumers
{
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

            ConsumerConfig consumerConfig = new()
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
            _consumer.Subscribe("OrderCreated");
            _logger.LogInformation("Subscribed to topic: OrderCreated");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(() => _logger.LogInformation("Cancellation requested, stopping the consumer..."));

            try
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
                            IPaymentRepository _paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

                            Payment result = await _paymentRepository.CreatePayment(orderMessage.Id, orderMessage.Amount);

                            _logger.LogInformation($"Order has been updated. New order with ID {result.Id}, status is: {result.Status}");
                        }

                        _consumer.Commit(consumeResult);

                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError($"Consume error: {ex.Error.Reason}");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                    catch (KafkaException kEx)
                    {
                        _logger.LogError($"Kafka error: {kEx.Error.Reason}");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                    catch (ObjectDisposedException ex)
                    {
                        _logger.LogError($"Consumer has been disposed: {ex.Message}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Unexpected error: {ex.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    }
                }
            }
            finally
            {
                _consumer.Close();
                _logger.LogInformation("Consumer closed gracefully.");
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Kafka consumer...");
            return base.StopAsync(cancellationToken);
        }
    }
}
