using Confluent.Kafka;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Stingray.Application.Commands;
using Stingray.Application.DTOs;
using Stingray.Application.Interfaces;
using Stingray.Application.Queries;
using Stingray.Domain.Interfaces;
using Stingray.Services.OrderService.Infrastructure;
using Stingray.Storage.InMemory;
using Stingray.Storage.InMemory.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommand).Assembly));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(CreateOrderCommand).Assembly);

// Add DbContext
builder.Services.AddDbContext<StingrayDbContext>(options =>
    options.UseInMemoryDatabase("OrderServiceDb"));

// Add Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();

// Configure Kafka Producer
var producerConfig = new ProducerConfig
{
    BootstrapServers = builder.Configuration.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092"
};
builder.Services.AddSingleton<IProducer<string, string>>(sp =>
    new ProducerBuilder<string, string>(producerConfig).Build());

// Add Event Publisher
builder.Services.AddScoped<IEventPublisher, KafkaEventPublisher>();

// Add Background Services
builder.Services.AddHostedService<UserCreatedEventConsumer>();

var app = builder.Build();

// Configure the HTTP request pipeline
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

// Minimal API endpoints
app.MapPost("/orders", async (CreateOrderRequest request, IMediator mediator, IValidator<CreateOrderCommand> validator) =>
    {
        var command = new CreateOrderCommand
        {
            UserId = request.UserId,
            ProductName = request.ProductName,
            Quantity = request.Quantity,
            Price = request.Price
        };

        var validationResult = await validator.ValidateAsync(command);
        if (!validationResult.IsValid) return Results.ValidationProblem(validationResult.ToDictionary());

        var result = await mediator.Send(command);
        return Results.Created($"/orders/{result.Id}", result);
    })
    .WithName("CreateOrder")
    .WithOpenApi();

app.MapGet("/orders/{id:guid}", async (Guid id, IMediator mediator) =>
    {
        var query = new GetOrderByIdQuery { OrderId = id };
        var result = await mediator.Send(query);
        return result != null ? Results.Ok(result) : Results.NotFound();
    })
    .WithName("GetOrder")
    .WithOpenApi();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OrderService" }))
    .WithName("HealthCheck")
    .WithOpenApi();

app.Run();