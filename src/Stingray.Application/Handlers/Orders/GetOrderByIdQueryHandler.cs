using MediatR;
using Stingray.Application.DTOs;
using Stingray.Application.Queries;
using Stingray.Domain.Interfaces;

namespace Stingray.Application.Handlers.Orders;

public class GetOrderByIdQueryHandler(IOrderRepository orderRepository) : IRequestHandler<GetOrderByIdQuery, OrderResponse?>
{
    public async Task<OrderResponse?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order == null)
            return null;

        return new OrderResponse
        {
            Id = order.Id,
            UserId = order.UserId,
            ProductName = order.ProductName,
            Quantity = order.Quantity,
            Price = order.Price,
            CreatedAt = order.CreatedAt
        };
    }
}