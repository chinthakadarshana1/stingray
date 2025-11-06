using MediatR;

namespace Stingray.Application.Notifications.Users;

public class UserCreatedNotification : INotification
{
    public Guid UserId { get; init; }
}