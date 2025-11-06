using MediatR;
using Stingray.Application.DTOs;

namespace Stingray.Application.Commands.Orders;

public class CreateOrderCommand : IRequest<OrderResponse>
{
    public Guid UserId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Price { get; init; }
}