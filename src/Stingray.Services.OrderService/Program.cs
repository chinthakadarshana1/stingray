using Confluent.Kafka;
using FluentValidation;
using Hangfire;
using Hangfire.InMemory;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using Stingray.Application.Commands.Orders;
using Stingray.Application.DTOs;
using Stingray.Application.Interfaces;
using Stingray.Application.Queries.Orders;
using Stingray.Domain.Interfaces;
using Stingray.Infrastructure.KafkaMessaging;
using Stingray.Services.OrderService.Consumers;
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
builder.Services.AddScoped<IEventPublisher, KafkaEventPublisher>();

// Configure Kafka Producer
var producerConfig = new ProducerConfig
{
    BootstrapServers = builder.Configuration.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092"
};
builder.Services.AddSingleton<IProducer<string, string>>(_ =>
    new ProducerBuilder<string, string>(producerConfig).Build());

// Configure Polly Resilience Pipeline for Kafka consumer
builder.Services.AddSingleton<ResiliencePipeline>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<UserCreatedEventConsumerJob>>();
    
    return new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<ConsumeException>(ex =>
                ex.Error.Code == ErrorCode.UnknownTopicOrPart ||
                ex.Error.Code == ErrorCode.Local_AllBrokersDown ||
                ex.Error.Code == ErrorCode.BrokerNotAvailable),
            MaxRetryAttempts = int.MaxValue, // Retry indefinitely
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            MaxDelay = TimeSpan.FromSeconds(30),
            OnRetry = args =>
            {
                var ex = args.Outcome.Exception as ConsumeException;
                logger.LogWarning(
                    "Kafka error (attempt {AttemptNumber}). Will retry in {RetryDelay}. Error: {ErrorCode} - {ErrorReason}",
                    args.AttemptNumber,
                    args.RetryDelay,
                    ex?.Error.Code,
                    ex?.Error.Reason);
                return default;
            }
        })
        .Build();
});

// Configure Hangfire with In-Memory storage (for development)
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseInMemoryStorage());

// Add Hangfire server
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1; // Number of concurrent workers
    options.ServerName = "OrderService-Kafka-Consumer";
    options.Queues = new[] { "kafka-consumers", "default" }; // Process kafka-consumers queue first
});

// Register the Kafka consumer job
builder.Services.AddScoped<UserCreatedEventConsumerJob>();

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

// Configure Hangfire Dashboard - must be before UseRouting if using routing
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    DashboardTitle = "OrderService Jobs"
    // In production, add authorization: Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Enqueue indefinite background job to process Kafka messages
// This will run continuously and retry after 10 seconds on any error
BackgroundJob.Enqueue<UserCreatedEventConsumerJob>(
    job => job.ProcessMessagesIndefinitely(CancellationToken.None));


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