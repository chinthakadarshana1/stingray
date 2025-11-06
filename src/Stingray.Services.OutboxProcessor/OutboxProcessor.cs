using Confluent.Kafka;
using Stingray.Domain.Interfaces;

namespace Stingray.Services.OutboxProcessor;

/// <summary>
///     Background service that processes unprocessed outbox messages and publishes them to Kafka.
///     Runs every 5 seconds to ensure reliable event delivery.
/// </summary>
public class OutboxProcessor(
    IServiceProvider serviceProvider,
    IProducer<string, string>? producer,
    ILogger<OutboxProcessor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

                var unprocessedMessages = await outboxRepository.GetUnprocessedMessagesAsync(stoppingToken);

                foreach (var message in unprocessedMessages)
                    try
                    {
                        var kafkaMessage = new Message<string, string>
                        {
                            Key = message.Id.ToString(),
                            Value = message.Payload
                        };

                        await outboxRepository.MarkAsProcessedAsync(message.Id, stoppingToken);
                        await producer!.ProduceAsync(message.EventType.ToString(), kafkaMessage, stoppingToken);
                        logger.LogInformation("Outbox message {MessageId} processed successfully", message.Id);
                    }
                    catch (Exception ex)
                    {
                        await outboxRepository.MarkAsErrorAsync(message.Id, stoppingToken);
                        logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
                    }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in outbox processor");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        logger.LogInformation("OutboxProcessor stopped");
    }

    public override void Dispose()
    {
        producer?.Dispose();
        base.Dispose();
    }
}