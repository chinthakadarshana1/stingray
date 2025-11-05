using System.Text.Json;
using Confluent.Kafka;
using Stingray.Domain.Events;

namespace Stingray.Services.OrderService.Infrastructure;

public class UserCreatedEventConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<UserCreatedEventConsumer> _logger;

    public UserCreatedEventConsumer(IConfiguration configuration, ILogger<UserCreatedEventConsumer> logger)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092",
            GroupId = "order-service-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("UserCreated");
        _logger.LogInformation("UserCreatedEventConsumer started and subscribed to UserCreated topic");

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);

                if (consumeResult?.Message?.Value != null)
                {
                    var userCreatedEvent = JsonSerializer.Deserialize<UserCreatedEvent>(consumeResult.Message.Value);

                    if (userCreatedEvent != null)
                        _logger.LogInformation(
                            $"Received UserCreated event for user {userCreatedEvent.UserId} - {userCreatedEvent.Email}");
                    // Here you could process the event, e.g., create a user cache entry
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Error consuming message from Kafka");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing UserCreated event");
            }

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}