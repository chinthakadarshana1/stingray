namespace Stingray.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(string eventType, T eventData, CancellationToken cancellationToken = default);
}