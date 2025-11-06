using MediatR;
using Microsoft.Extensions.Logging;
using Stingray.Domain.Interfaces;

namespace Stingray.Application.Notifications.Users;

public class UserCreatedNotificationHandler(ILogger<UserCreatedNotificationHandler> logger, IUserRepository userRepository)
    : INotificationHandler<UserCreatedNotification>
{
    public async Task Handle(UserCreatedNotification notification, CancellationToken cancellationToken)
    {
        var userInfo = await userRepository.GetByIdAsync(notification.UserId, cancellationToken);
        if (userInfo != null)
            logger.LogInformation(
                "Received UserCreated event for user {UserId} - {Email}",
                userInfo.Id,
                userInfo.Email);
    }
}