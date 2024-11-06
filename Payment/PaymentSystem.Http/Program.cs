using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using PaymentSystem.Http.Middleware;
using PaymentSystem.Infra;
using PaymentSystem.Application;
using PaymentSystem.Presentation.Api.Routes;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddInfrastructure(builder.Configuration)
                .AddApplication();


builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPaymentEndpoint();


app.Run();
