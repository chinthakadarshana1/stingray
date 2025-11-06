using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Stingray.Application.Commands.Users;
using Stingray.Application.DTOs;
using Stingray.Application.Interfaces;
using Stingray.Domain.Interfaces;
using Stingray.Domain.Outbox;

namespace Stingray.Tests.Application.Handlers.Users;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();

        _handler = new CreateUserCommandHandler(
            _userRepositoryMock.Object,
            _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateUser_WhenValidCommand()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Email = "test@example.com",
            Name = "Test User"
        };

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.User user, CancellationToken ct) => user);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<EventType>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.Email.Should().Be(command.Email);
        result.Name.Should().Be(command.Name);

        _userRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<Domain.User>(u => 
                    u.Email == command.Email && 
                    u.Name == command.Name),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                EventType.UserCreated,
                It.Is<object>(e => 
                    e.GetType().GetProperty("Email")!.GetValue(e)!.ToString() == command.Email),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPublishUserCreatedEvent_WithCorrectData()
    {
        // Arrange
        var command = new CreateUserCommand
        {
            Email = "john.doe@example.com",
            Name = "John Doe"
        };

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.User user, CancellationToken ct) => user);

        object? publishedEvent = null;
        _eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<EventType>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventType, object, CancellationToken>((type, evt, ct) => publishedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        publishedEvent.Should().NotBeNull();
        
        var userIdProp = publishedEvent!.GetType().GetProperty("UserId");
        var emailProp = publishedEvent.GetType().GetProperty("Email");
        var nameProp = publishedEvent.GetType().GetProperty("Name");

        userIdProp!.GetValue(publishedEvent).Should().NotBeNull();
        emailProp!.GetValue(publishedEvent).Should().Be(command.Email);
        nameProp!.GetValue(publishedEvent).Should().Be(command.Name);
    }

    [Fact]
    public async Task Handle_ShouldGenerateUniqueUserId_ForEachUser()
    {
        // Arrange
        var command1 = new CreateUserCommand { Email = "user1@example.com", Name = "User 1" };
        var command2 = new CreateUserCommand { Email = "user2@example.com", Name = "User 2" };

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.User user, CancellationToken ct) => user);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EventType>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result1 = await _handler.Handle(command1, CancellationToken.None);
        var result2 = await _handler.Handle(command2, CancellationToken.None);

        // Assert
        result1.Id.Should().NotBe(result2.Id);
        result1.Id.Should().NotBeEmpty();
        result2.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryBeforePublishingEvent()
    {
        // Arrange
        var command = new CreateUserCommand { Email = "test@example.com", Name = "Test" };
        
        var callOrder = new List<string>();
        
        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.User>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("Repository"))
            .ReturnsAsync((Domain.User user, CancellationToken ct) => user);

        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<EventType>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("EventPublisher"))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        callOrder.Should().Equal("Repository", "EventPublisher");
    }
}

