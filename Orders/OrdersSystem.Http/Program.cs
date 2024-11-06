using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using OrdersSystem.Infra;
using OrdersSystem.Http.Middleware;
using OrdersSystem.Presentation.Api.Routes;
using OrdersSystem.Application;

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


app.MapOrderEndpoint();

app.Run();
