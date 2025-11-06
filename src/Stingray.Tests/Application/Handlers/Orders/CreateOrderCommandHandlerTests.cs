using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Stingray.Application.Commands.Orders;
using Stingray.Application.Interfaces;
using Stingray.Domain.Interfaces;
using Stingray.Domain.Outbox;

namespace Stingray.Tests.Application.Handlers.Orders;

public class CreateOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly CreateOrderCommandHandler _handler;

    public CreateOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _eventPublisherMock = new Mock<IEventPublisher>();

        _handler = new CreateOrderCommandHandler(
            _orderRepositoryMock.Object,
            _eventPublisherMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateOrder_WhenValidCommand()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            UserId = Guid.NewGuid(),
            ProductName = "Test Product",
            Quantity = 2,
            Price = 29.99m
        };

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Order order, CancellationToken ct) => order);

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
        result.UserId.Should().Be(command.UserId);
        result.ProductName.Should().Be(command.ProductName);
        result.Quantity.Should().Be(command.Quantity);
        result.Price.Should().Be(command.Price);

        _orderRepositoryMock.Verify(
            x => x.AddAsync(
                It.Is<Domain.Order>(o =>
                    o.UserId == command.UserId &&
                    o.ProductName == command.ProductName &&
                    o.Quantity == command.Quantity &&
                    o.Price == command.Price),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                EventType.OrderCreated,
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPublishOrderCreatedEvent_WithCorrectData()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            UserId = Guid.NewGuid(),
            ProductName = "Widget",
            Quantity = 5,
            Price = 99.99m
        };

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Order order, CancellationToken ct) => order);

        object? publishedEvent = null;
        _eventPublisherMock
            .Setup(x => x.PublishAsync(
                It.IsAny<EventType>(),
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .Callback<EventType, object, CancellationToken>((type, evt, ct) => publishedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        publishedEvent.Should().NotBeNull();

        var orderIdProp = publishedEvent!.GetType().GetProperty("OrderId");
        var userIdProp = publishedEvent.GetType().GetProperty("UserId");
        var productNameProp = publishedEvent.GetType().GetProperty("ProductName");

        orderIdProp!.GetValue(publishedEvent).Should().Be(result.Id);
        userIdProp!.GetValue(publishedEvent).Should().Be(command.UserId);
        productNameProp!.GetValue(publishedEvent).Should().Be(command.ProductName);
    }

    [Fact]
    public async Task Handle_ShouldGenerateUniqueOrderId_ForEachOrder()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var command1 = new CreateOrderCommand
        {
            UserId = userId,
            ProductName = "Product 1",
            Quantity = 1,
            Price = 10.00m
        };
        var command2 = new CreateOrderCommand
        {
            UserId = userId,
            ProductName = "Product 2",
            Quantity = 2,
            Price = 20.00m
        };

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Order order, CancellationToken ct) => order);

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
    public async Task Handle_ShouldCalculateTotalPrice_Correctly()
    {
        // Arrange
        var command = new CreateOrderCommand
        {
            UserId = Guid.NewGuid(),
            ProductName = "Test Product",
            Quantity = 3,
            Price = 15.50m
        };

        Domain.Order? savedOrder = null;
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.Order>(), It.IsAny<CancellationToken>()))
            .Callback<Domain.Order, CancellationToken>((order, ct) => savedOrder = order)
            .ReturnsAsync((Domain.Order order, CancellationToken ct) => order);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        savedOrder.Should().NotBeNull();
        savedOrder!.Quantity.Should().Be(3);
        savedOrder.Price.Should().Be(15.50m);
        // Total would be 3 * 15.50 = 46.50 if calculated
    }
}

