using Microsoft.EntityFrameworkCore;
using Stingray.Domain.Interfaces;
using Stingray.Domain.Outbox;

namespace Stingray.Storage.InMemory.Repositories;

public class OutboxRepository(StingrayDbContext context) : IOutboxRepository
{
    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default)
    {
        return await context.OutboxMessages
            .Where(m => !m.Status.Equals(OutboxMessageStatus.Processed))
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await context.OutboxMessages.FindAsync([id], cancellationToken);
        if (message != null)
        {
            message.Status = OutboxMessageStatus.Processed;
            message.ProcessedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAsErrorAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var message = await context.OutboxMessages.FindAsync([id], cancellationToken);
        if (message != null)
        {
            message.Status = OutboxMessageStatus.Failed;
            message.ProcessedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}