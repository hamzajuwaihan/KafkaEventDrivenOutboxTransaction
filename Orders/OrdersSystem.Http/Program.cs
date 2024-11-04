using OrdersSystem.Domain.Entities;
using OrdersSystem.Domain.Enums;
using OrdersSystem.Infra.DB;
using OrdersSystem.Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Confluent.Kafka;
using OrdersSystem.Infra.Producers;
using OrdersSystem.Infra.Processors;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var kafkaConfig = builder.Configuration.GetSection("Kafka").Get<ProducerConfig>();

builder.Services.AddSingleton(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KafkaProducer>>();
    return new KafkaProducer(kafkaConfig, logger);
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<OrdersRepository>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddHostedService<OutboxProcessor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/Order", async (OrdersRepository ordersRepository, KafkaProducer kafkaProducer, int amount) =>
{
    var orderId = Guid.NewGuid();
    Order order = new()
    {
        Id = orderId,
        Amount = amount,
        Status = OrderStatus.Pending,
    };

    await ordersRepository.CreateOrder(order);

    // Publish "Order Created" event to Kafka, including the order ID in the message
    await kafkaProducer.ProduceAsync("orders-topic", $"Order created with ID {orderId}, Amount: {amount}");

    return Results.Created($"/Order/{orderId}", order);
})
.WithName("Create Order")
.WithOpenApi();

// Endpoint for getting an order by ID
app.MapGet("/Order/{id:guid}", async (OrdersRepository ordersRepository, Guid id) =>
{
    var order = await ordersRepository.GetOrder(id);

    if (order is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(order);
})
.WithName("Get Order")
.WithOpenApi();

app.Run();
