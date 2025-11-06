using FluentAssertions;
using Moq;
using Stingray.Application.Queries.Orders;
using Stingray.Domain.Interfaces;

namespace Stingray.Tests.Application.Handlers.Orders;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly GetOrderByIdQueryHandler _handler;

    public GetOrderByIdQueryHandlerTests()
    {
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _handler = new GetOrderByIdQueryHandler(_orderRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnOrder_WhenOrderExists()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expectedOrder = new Domain.Order
        {
            Id = orderId,
            UserId = userId,
            ProductName = "Test Product",
            Quantity = 3,
            Price = 49.99m
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        var query = new GetOrderByIdQuery { OrderId = orderId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(orderId);
        result.UserId.Should().Be(userId);
        result.ProductName.Should().Be("Test Product");
        result.Quantity.Should().Be(3);
        result.Price.Should().Be(49.99m);

        _orderRepositoryMock.Verify(
            x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNull_WhenOrderDoesNotExist()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Order?)null);

        var query = new GetOrderByIdQuery { OrderId = orderId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        _orderRepositoryMock.Verify(
            x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMapAllOrderProperties_Correctly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var order = new Domain.Order
        {
            Id = orderId,
            UserId = userId,
            ProductName = "Premium Widget",
            Quantity = 10,
            Price = 199.99m
        };

        _orderRepositoryMock
            .Setup(x => x.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var query = new GetOrderByIdQuery { OrderId = orderId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(order.Id);
        result.UserId.Should().Be(order.UserId);
        result.ProductName.Should().Be(order.ProductName);
        result.Quantity.Should().Be(order.Quantity);
        result.Price.Should().Be(order.Price);
    }
}

