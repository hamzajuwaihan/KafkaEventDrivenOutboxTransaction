using System;
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
            
            using (var scope = _serviceProvider.CreateScope()) 
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>(); 

                var messages = await context.OutboxMessages
                    .Where(m => !m.IsProcessed)
                    .ToListAsync(stoppingToken);

                foreach (var message in messages)
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

