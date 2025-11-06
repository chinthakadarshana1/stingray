using System.Text.Json;
using Confluent.Kafka;
using Hangfire;
using MediatR;
using Polly;
using Stingray.Application.Notifications.Users;
using Stingray.Domain.Events;
using Stingray.Domain.Outbox;

namespace Stingray.Services.OrderService.Consumers;

/// <summary>
///     Kafka consumer job that processes UserCreated events
///     Executed as a Hangfire recurring job
/// </summary>
public class UserCreatedEventConsumerJob
{
    private readonly IConfiguration _configuration;
    private readonly IConsumer<string, string>? _consumer;
    private readonly ILogger<UserCreatedEventConsumerJob> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly IServiceProvider _serviceProvider;

    public UserCreatedEventConsumerJob(
        ILogger<UserCreatedEventConsumerJob> logger,
        IServiceProvider serviceProvider,
        ResiliencePipeline resiliencePipeline,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _resiliencePipeline = resiliencePipeline;
        _configuration = configuration;

        if (_consumer != null) return;

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _configuration.GetValue<string>("Kafka:BootstrapServers") ?? "kafka:9092",
            GroupId = "order-service-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            AutoCommitIntervalMs = 5000
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

        var topicName = nameof(EventType.UserCreated);

        // Subscribe with Polly retry policy
        _resiliencePipeline.Execute(_ =>
        {
            _consumer.Subscribe(topicName);
            _logger.LogInformation("UserCreatedEventConsumerJob subscribed to topic: {TopicName}", topicName);
        });
    }


    /// <summary>
    ///     Processes messages from Kafka indefinitely (called continuously by Hangfire)
    ///     If an error occurs, Hangfire will retry after 10 seconds
    /// </summary>
    [AutomaticRetry(Attempts = int.MaxValue, DelaysInSeconds = new[] { 10 })]
    [Queue("kafka-consumers")]
    public async Task ProcessMessagesIndefinitely(CancellationToken cancellationToken = default)
    {
        if (_consumer == null)
        {
            _logger.LogWarning("Consumer not initialized, will retry in 10 seconds");
            throw new InvalidOperationException("Consumer not initialized");
        }

        _logger.LogInformation("Starting indefinite message processing loop");

        // Run indefinitely until cancelled
        while (!cancellationToken.IsCancellationRequested)
            try
            {
                // Try to consume a message with Polly retry policy
                var consumeResult = await _resiliencePipeline.ExecuteAsync(
                    async ct => await Task.Run(() => _consumer.Consume(TimeSpan.FromMilliseconds(500)), ct),
                    cancellationToken);

                if (consumeResult == null || consumeResult.IsPartitionEOF)
                {
                    // No messages available, short pause before next attempt
                    await Task.Delay(1000, cancellationToken);
                    continue;
                }

                if (consumeResult.Message?.Value != null)
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
                        }, cancellationToken);

                        _logger.LogInformation(
                            "Successfully processed UserCreated event for user {UserId}",
                            userCreatedEvent.UserId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("UserCreatedEventConsumerJob cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing UserCreated event, continuing to next message");
                // Continue to next message instead of breaking the loop
                await Task.Delay(100, cancellationToken);
            }

        _logger.LogInformation("Message processing loop ended");
    }

    /// <summary>
    ///     Cleanup resources
    /// </summary>
    public void Dispose()
    {
        if (_consumer != null)
            try
            {
                _consumer.Close();
                _consumer.Dispose();
                _logger.LogInformation("UserCreatedEventConsumerJob disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing consumer");
            }
    }
}