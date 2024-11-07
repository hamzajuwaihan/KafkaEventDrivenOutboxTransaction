using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentSystem.Domain.Messages;
using PaymentSystem.Infra.RepositoriesContracts;

namespace PaymentSystem.Infra.Consumers
{
    /// <summary>
    /// Consumer for OrderCreated Kafka topic
    /// </summary>
    public class OrderCreatedConsumer : BackgroundService
    {
        private readonly IConsumer<Ignore, string> _consumer;
        private readonly ILogger<OrderCreatedConsumer> _logger;
        private readonly IServiceProvider _serviceProvider;

        public OrderCreatedConsumer(
            IConfiguration configuration,
            ILogger<OrderCreatedConsumer> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;

            string? bootstrapServers = configuration["Kafka:BootstrapServers"];
            string? groupId = configuration["Kafka:Consumers:OrderCreatedConsumer:GroupId"];
            string? autoOffsetResetString = configuration["Kafka:Consumers:OrderCreatedConsumer:AutoOffsetReset"];

            if (!Enum.TryParse(autoOffsetResetString, true, out AutoOffsetReset autoOffsetReset))
            {
                autoOffsetReset = AutoOffsetReset.Earliest;
                _logger.LogWarning("Invalid AutoOffsetReset value in configuration. Defaulting to 'Earliest'.");
            }

            ConsumerConfig consumerConfig = new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = autoOffsetReset,
                EnableAutoCommit = true,
                SessionTimeoutMs = 10000,
                ReconnectBackoffMs = 1000,
                ReconnectBackoffMaxMs = 10000,
                SocketKeepaliveEnable = true
            };

            _consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
            _consumer.Subscribe("OrderCreated");
            _logger.LogInformation("Subscribed to topic: OrderCreated");
        }

        /// <summary>
        /// Consumes messages from the OrderCreated topic
        /// </summary>
        /// <param name="stoppingToken">Cancellation token for stopping the consumer</param>
        /// <returns>A task that represents the consumer's execution</returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() =>
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

                            ProcessMessage(consumeResult.Message.Value, stoppingToken);

                            _consumer.Commit(consumeResult);
                        }
                        catch (ConsumeException ex)
                        {
                            _logger.LogError($"Consume error: {ex.Error.Reason}");
                            Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).Wait(stoppingToken);
                        }
                        catch (KafkaException kEx)
                        {
                            _logger.LogError($"Kafka error: {kEx.Error.Reason}");
                            Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).Wait(stoppingToken);
                        }
                        catch (ObjectDisposedException ex)
                        {
                            _logger.LogError($"Consumer has been disposed: {ex.Message}");
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Unexpected error: {ex.Message}");
                            Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).Wait(stoppingToken);
                        }
                    }
                }
                finally
                {
                    _consumer.Close();
                    _logger.LogInformation("Consumer closed gracefully.");
                }
            }, stoppingToken);
        }

        private void ProcessMessage(string messageValue, CancellationToken cancellationToken)
        {
            try
            {
                // Deserialize the message
                OrderCreatedMessage? orderMessage = JsonSerializer.Deserialize<OrderCreatedMessage>(messageValue);

                if (orderMessage != null)
                {
                    using IServiceScope scope = _serviceProvider.CreateScope();
                    IPaymentRepository paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

                    Domain.Entities.Payment result = paymentRepository.CreatePayment(orderMessage.Id, orderMessage.Amount).Result;

                    _logger.LogInformation($"Order has been created. New order with ID {result.Id}, status is: {result.Status}");
                }
                else
                {
                    _logger.LogWarning("OrderCreatedMessage deserialization resulted in null.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing message: {ex.Message}");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Kafka consumer...");
            return base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _consumer.Dispose();
            base.Dispose();
        }
    }
}
