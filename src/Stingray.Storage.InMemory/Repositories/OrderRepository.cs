using Stingray.Domain;
using Stingray.Domain.Interfaces;

namespace Stingray.Storage.InMemory.Repositories;

public class OrderRepository(StingrayDbContext context) : IOrderRepository
{
    public async Task<Order> AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);
        return order;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Orders.FindAsync([id], cancellationToken);
    }
}