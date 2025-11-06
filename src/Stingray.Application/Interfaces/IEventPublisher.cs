using Stingray.Domain.Outbox;

namespace Stingray.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(EventType eventType, T eventData, CancellationToken cancellationToken = default);
}