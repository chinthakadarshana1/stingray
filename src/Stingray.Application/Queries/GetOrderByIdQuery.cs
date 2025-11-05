using MediatR;
using Stingray.Application.DTOs;

namespace Stingray.Application.Queries;

public class GetOrderByIdQuery : IRequest<OrderResponse?>
{
    public Guid OrderId { get; init; }
}