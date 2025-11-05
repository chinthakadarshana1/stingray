using MediatR;
using Stingray.Application.Commands;
using Stingray.Application.DTOs;
using Stingray.Application.Interfaces;
using Stingray.Domain;
using Stingray.Domain.Events;
using Stingray.Domain.Interfaces;

namespace Stingray.Application.Handlers.Users;

public class CreateUserCommandHandler(IUserRepository userRepository, IEventPublisher eventPublisher)
    : IRequestHandler<CreateUserCommand, UserResponse>
{
    public async Task<UserResponse> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await userRepository.AddAsync(user, cancellationToken);

        // Publish UserCreated event
        var userCreatedEvent = new UserCreatedEvent
        {
            UserId = createdUser.Id,
            Email = createdUser.Email,
            Name = createdUser.Name,
            CreatedAt = createdUser.CreatedAt
        };

        await eventPublisher.PublishAsync("UserCreated", userCreatedEvent, cancellationToken);

        return new UserResponse
        {
            Id = createdUser.Id,
            Email = createdUser.Email,
            Name = createdUser.Name,
            CreatedAt = createdUser.CreatedAt
        };
    }
}