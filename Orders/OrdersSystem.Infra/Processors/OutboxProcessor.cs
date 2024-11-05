using Microsoft.EntityFrameworkCore;
using OrdersSystem.Infra.DB;
using Microsoft.Extensions.Hosting;
using OrdersSystem.Infra.Producers;
using Microsoft.Extensions.DependencyInjection;

namespace OrdersSystem.Infra.Processors;
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaProducer _kafkaProducer;

    public OutboxProcessor(IServiceProvider serviceProvider, KafkaProducer kafkaProducer)
    {
        _serviceProvider = serviceProvider;
        _kafkaProducer = kafkaProducer;
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
                    .ToListAsync(stoppingToken);

                foreach (Domain.Entities.OutboxMessage? message in messages)
                {
                    await _kafkaProducer.ProduceAsync(message.Topic, message.Message);
                    message.IsProcessed = true; 
                }

                await context.SaveChangesAsync(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
