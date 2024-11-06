using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentSystem.Domain.Entities;
using PaymentSystem.Infra.DB;
using PaymentSystem.Infra.Producers;

namespace PaymentSystem.Infra.Processors;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaProducer _kafkaProducer;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(IServiceProvider serviceProvider, KafkaProducer kafkaProducer, ILogger<OutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                List<OutboxMessage> messages = await context.OutboxMessages
                    .Where(m => !m.IsProcessed)
                    .ToListAsync(stoppingToken);

                foreach (OutboxMessage? message in messages)
                {
                    try
                    {  
                        _logger.LogInformation($"Trying to produce the following message topic: {message.Topic}, message: {message.Message}");
                        await _kafkaProducer.ProduceAsync(message.Topic, message.Message).ConfigureAwait(false);
                        message.IsProcessed = true;
                        _logger.LogInformation($"Finished producing the msg with message ID: {message.Id}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message with ID: {MessageId}", message.Id);
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
