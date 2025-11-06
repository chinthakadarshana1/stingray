using MediatR;
using Stingray.Application.DTOs;

namespace Stingray.Application.Commands.Users;

public class CreateUserCommand : IRequest<UserResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}