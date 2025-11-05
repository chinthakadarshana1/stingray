using MediatR;
using Stingray.Application.DTOs;

namespace Stingray.Application.Queries;

public class GetUserByIdQuery : IRequest<UserResponse?>
{
    public Guid UserId { get; init; }
}