namespace Stingray.Application.Interfaces;

public interface IEventConsumer
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}