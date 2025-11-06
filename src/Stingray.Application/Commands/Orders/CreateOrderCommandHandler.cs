using MediatR;
using Stingray.Application.DTOs;
using Stingray.Application.Interfaces;
using Stingray.Domain;
using Stingray.Domain.Events;
using Stingray.Domain.Interfaces;
using Stingray.Domain.Outbox;

namespace Stingray.Application.Commands.Orders;

public class CreateOrderCommandHandler(IOrderRepository orderRepository, IEventPublisher eventPublisher)
    : IRequestHandler<CreateOrderCommand, OrderResponse>
{
    public async Task<OrderResponse> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            ProductName = request.ProductName,
            Quantity = request.Quantity,
            Price = request.Price,
            CreatedAt = DateTime.UtcNow
        };

        var createdOrder = await orderRepository.AddAsync(order, cancellationToken);

        // Publish OrderCreated event
        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = createdOrder.Id,
            UserId = createdOrder.UserId,
            ProductName = createdOrder.ProductName,
            Quantity = createdOrder.Quantity,
            TotalPrice = createdOrder.Price,
            CreatedAt = createdOrder.CreatedAt
        };

        await eventPublisher.PublishAsync(EventType.OrderCreated, orderCreatedEvent, cancellationToken);

        return new OrderResponse
        {
            Id = createdOrder.Id,
            UserId = createdOrder.UserId,
            ProductName = createdOrder.ProductName,
            Quantity = createdOrder.Quantity,
            Price = createdOrder.Price,
            CreatedAt = createdOrder.CreatedAt
        };
    }
}