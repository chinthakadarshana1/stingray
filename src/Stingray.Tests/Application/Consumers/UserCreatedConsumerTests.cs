using FluentAssertions;
using Stingray.Application.Notifications.Users;

namespace Stingray.Tests.Application.Consumers;

/// <summary>
///     Placeholder for UserCreatedConsumer tests
///     The consumer class needs to be implemented in Stingray.Application.Consumers
/// </summary>
public class UserCreatedNotificationTests
{
    [Fact]
    public void UserCreatedNotification_ShouldHaveUserId()
    {
        // Arrange & Act
        var userId = Guid.NewGuid();
        var notification = new UserCreatedNotification
        {
            UserId = userId
        };

        // Assert
        notification.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserCreatedNotification_ShouldBeInitializable()
    {
        // Act
        var notification = new UserCreatedNotification();

        // Assert
        notification.Should().NotBeNull();
        notification.UserId.Should().Be(Guid.Empty);
    }
}