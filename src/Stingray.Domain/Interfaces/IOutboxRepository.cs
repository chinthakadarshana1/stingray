using Stingray.Domain.Outbox;

namespace Stingray.Domain.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default);
    Task MarkAsErrorAsync(Guid id, CancellationToken cancellationToken = default);
}