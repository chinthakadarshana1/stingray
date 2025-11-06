using MediatR;
using Stingray.Application.DTOs;

namespace Stingray.Application.Queries.Orders;

public class GetOrderByIdQuery : IRequest<OrderResponse?>
{
    public Guid OrderId { get; init; }
}