using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace OrdersSystem.Infra.Producers;
public class KafkaProducer
{
    private readonly ProducerConfig _config;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(ProducerConfig config, ILogger<KafkaProducer> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task ProduceAsync(string topic, string message)
    {
        using var producer = new ProducerBuilder<Null, string>(_config).Build();
        try
        {
            var deliveryResult = await producer.ProduceAsync(topic, new Message<Null, string> { Value = message });

            // Log message with detailed information
            _logger.LogInformation($"Delivered message for Order ID: {message} to topic '{topic}' at offset {deliveryResult.Offset}");
        }
        catch (ProduceException<Null, string> e)
        {
            _logger.LogError($"Delivery failed for message: {message}. Error: {e.Error.Reason}");
        }
    }
}