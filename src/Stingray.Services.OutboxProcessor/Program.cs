using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Stingray.Domain.Interfaces;
using Stingray.Services.OutboxProcessor;
using Stingray.Storage.InMemory;
using Stingray.Storage.InMemory.Repositories;

var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true)
    .AddEnvironmentVariables();

// Add DbContext - Connect to the databases that need outbox processing
// In a real scenario, you might connect to multiple databases or use a shared outbox database
var dbConnectionString = builder.Configuration.GetValue<string>("DatabaseName") ?? "OutboxProcessorDb";
builder.Services.AddDbContext<StingrayDbContext>(options =>
    options.UseInMemoryDatabase(dbConnectionString));

// Add Repositories
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();

// Configure Kafka Producer
var kafkaBootstrapServers = builder.Configuration.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092";
var producerConfig = new ProducerConfig
{
    BootstrapServers = kafkaBootstrapServers,
    EnableIdempotence = true,
    Acks = Acks.All,
    MessageSendMaxRetries = 3,
    RetryBackoffMs = 1000
};

builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Initializing Kafka Producer with bootstrap servers: {BootstrapServers}", kafkaBootstrapServers);
    return new ProducerBuilder<string, string>(producerConfig).Build();
});

// Register the OutboxProcessor as a hosted service
builder.Services.AddHostedService<OutboxProcessor>();

// Configure logging
builder.Logging.AddConsole();

var host = builder.Build();

// Log startup information
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("OutboxProcessor Service starting...");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
logger.LogInformation("Kafka Bootstrap Servers: {BootstrapServers}", kafkaBootstrapServers);
logger.LogInformation("Database: {DatabaseName}", dbConnectionString);

await host.RunAsync();