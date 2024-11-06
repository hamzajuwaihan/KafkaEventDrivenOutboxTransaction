using Microsoft.EntityFrameworkCore;
using OrdersSystem.Infra.DB;
using Microsoft.Extensions.Hosting;
using OrdersSystem.Infra.Producers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Storage;

namespace OrdersSystem.Infra.Processors;
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
                List<Domain.Entities.OutboxMessage> messages = await context.OutboxMessages
                    .Where(m => !m.IsProcessed)
                    .OrderBy(m => m.CreatedAt)
                    .Take(10) 
                    .ToListAsync(stoppingToken);

                if (messages.Any())
                {
                    using IDbContextTransaction transaction = context.Database.BeginTransaction();
                    try
                    {
                        foreach (Domain.Entities.OutboxMessage? message in messages)
                        {
                            await _kafkaProducer.ProduceAsync(message.Topic, message.Message);
                            message.IsProcessed = true;
                        }
                        await context.SaveChangesAsync(stoppingToken);
                        await transaction.CommitAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync(stoppingToken);
                        _logger.LogError(ex, "Failed to process messages. Transaction rolled back.");
                    }
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}

