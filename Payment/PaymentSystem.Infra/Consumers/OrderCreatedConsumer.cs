using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentSystem.Domain.Entities;
using PaymentSystem.Domain.Messages;
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

            ConsumerConfig consumerConfig = new()
            {
                BootstrapServers = configuration["Kafka:BootstrapServers"],
                GroupId = configuration["Kafka:Consumers:OrderCreatedConsumer:GroupId"],
                AutoOffsetReset = Enum.Parse<AutoOffsetReset>(configuration["Kafka:Consumers:OrderCreatedConsumer:AutoOffsetReset"] ?? "Earliest"),
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

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
                        OrderCreatedMessage? orderMessage = JsonSerializer.Deserialize<OrderCreatedMessage>(consumeResult.Message.Value);

                        if (orderMessage != null)
                        {
                            Payment result = await paymentRepository.CreatePayment(orderMessage.Id, orderMessage.Amount);
                            _logger.LogInformation($"Order has been updated. New order with ID {result.Id}, status is: {result.Status}");
                        }

                        _consumer.Commit(consumeResult);
                    }
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
            _consumer.Close();
            return base.StopAsync(cancellationToken);
        }
    }
}
