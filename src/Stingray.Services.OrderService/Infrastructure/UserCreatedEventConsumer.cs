using System.Text.Json;
using Confluent.Kafka;
using MediatR;
using Polly;
using Stingray.Application.Notifications.Users;
using Stingray.Domain.Events;
using Stingray.Domain.Outbox;

namespace Stingray.Services.OrderService.Infrastructure;

public class UserCreatedEventConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<UserCreatedEventConsumer> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly IServiceProvider _serviceProvider;

    public UserCreatedEventConsumer(
        IConfiguration configuration,
        ILogger<UserCreatedEventConsumer> logger,
        IServiceProvider serviceProvider,
        ResiliencePipeline resiliencePipeline)
    {
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = configuration.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092",
            GroupId = "order-service-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        _logger = logger;
        _serviceProvider = serviceProvider;
        _resiliencePipeline = resiliencePipeline;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topicName = nameof(EventType.UserCreated);

        _logger.LogInformation("UserCreatedEventConsumer starting, will subscribe to topic: {TopicName}", topicName);

        // Run the consumer loop in a background task to avoid blocking application startup
        await Task.Run(async () =>
        {
            // Subscribe with Polly retry policy
            await _resiliencePipeline.ExecuteAsync(_ =>
            {
                _consumer.Subscribe(topicName);
                _logger.LogInformation("UserCreatedEventConsumer subscribed to topic: {TopicName}", topicName);
                return ValueTask.CompletedTask;
            }, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
                try
                {
                    // Consume message with Polly retry policy for transient errors
                    var consumeResult =
                        await _resiliencePipeline.ExecuteAsync(async ct => { return await Task.Run(() => _consumer.Consume(ct), ct); },
                            stoppingToken);

                    if (consumeResult?.Message?.Value != null)
                    {
                        var userCreatedEvent = JsonSerializer.Deserialize<UserCreatedEvent>(consumeResult.Message.Value);

                        if (userCreatedEvent != null)
                        {
                            _logger.LogInformation(
                                "Received UserCreated event for user {Email}",
                                userCreatedEvent.Email);

                            // Create a scope to resolve scoped services (like IUserRepository)
                            using var scope = _serviceProvider.CreateScope();
                            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                            await mediator.Publish(new UserCreatedNotification
                            {
                                UserId = userCreatedEvent.UserId
                            }, stoppingToken);

                            _logger.LogInformation(
                                "Successfully processed UserCreated event for user {UserId}",
                                userCreatedEvent.UserId);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("UserCreatedEventConsumer cancellation requested");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing UserCreated event");
                    // Wait a bit before continuing to avoid tight loop on persistent errors
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
        }, stoppingToken);

        _logger.LogInformation("UserCreatedEventConsumer stopped");
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}