using System.Text.Json;
using Confluent.Kafka;
using Stingray.Application.Interfaces;
using Stingray.Domain.Interfaces;
using Stingray.Domain.Outbox;

namespace Stingray.Services.OrderService.Infrastructure;

public class KafkaEventPublisher(
    IProducer<string, string> producer,
    IOutboxRepository outboxRepository,
    ILogger<KafkaEventPublisher> logger)
    : IEventPublisher
{
    public async Task PublishAsync<T>(string eventType, T eventData, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(eventData);

        // Save to outbox first
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = EventType.OrderCreated,
            Payload = payload,
            CreatedAt = DateTime.UtcNow,
            Status = OutboxMessageStatus.Processing
        };

        await outboxRepository.AddAsync(outboxMessage, cancellationToken);

        // Try to publish immediately
        try
        {
            var message = new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = payload
            };

            await producer.ProduceAsync(eventType, message, cancellationToken);
            await outboxRepository.MarkAsProcessedAsync(outboxMessage.Id, cancellationToken);
            logger.LogInformation($"Event {eventType} published successfully");
        }
        catch (Exception ex)
        {
            await outboxRepository.MarkAsErrorAsync(outboxMessage.Id, cancellationToken);
            logger.LogError(ex, $"Failed to publish event {eventType}. Will retry from outbox.");
        }
    }
}