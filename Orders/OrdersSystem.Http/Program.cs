using OrdersSystem.Domain.Entities;
using OrdersSystem.Domain.Enums;
using OrdersSystem.Infra.DB;
using OrdersSystem.Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using OrdersSystem.Infra.Producers;
using OrdersSystem.Infra.Processors;
using OrdersSystem.Infra;
using OrdersSystem.Infra.Consumers;
using System.Text.Json;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddKafkaConsumer(builder.Configuration);
builder.Services.AddKafkaProducer(builder.Configuration);

builder.Services.AddScoped<OrdersRepository>();


builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddHostedService<PaymentProcessTopicConsumer>();

builder.Services.AddHostedService<OutboxProcessor>();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/Order", async (OrdersRepository ordersRepository, KafkaProducer kafkaProducer, int amount) =>
{
    Guid orderId = Guid.NewGuid();
    Order order = new()
    {
        Id = orderId,
        Amount = amount,
        Status = OrderStatus.Pending,
    };


    string message = JsonSerializer.Serialize(await ordersRepository.CreateOrder(order));

    await kafkaProducer.ProduceAsync("OrderCreated", message);

    return Results.Created($"/Order/{orderId}", order);
})
.WithName("Create Order")
.WithOpenApi();

app.MapGet("/Order/{id:guid}", async (OrdersRepository ordersRepository, Guid id) =>
{
    Order? order = await ordersRepository.GetOrder(id);

    if (order is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(order);
})
.WithName("Get Order")
.WithOpenApi();

app.Run();
