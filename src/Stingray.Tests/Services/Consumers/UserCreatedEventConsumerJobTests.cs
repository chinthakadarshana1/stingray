using FluentAssertions;
using Stingray.Services.OrderService.Consumers;

namespace Stingray.Tests.Services.Consumers;

/// <summary>
///     Integration tests for UserCreatedEventConsumerJob
///     Note: ResiliencePipeline is a sealed class and cannot be mocked,
///     so we focus on testing the public contract and attributes
/// </summary>
public class UserCreatedEventConsumerJobIntegrationTests
{
    [Fact]
    public void Job_ShouldHaveCorrectHangfireAttributes()
    {
        // Arrange & Act
        var method = typeof(UserCreatedEventConsumerJob)
            .GetMethod("ProcessMessagesIndefinitely");

        // Assert
        method.Should().NotBeNull();

        var attributes = method!.GetCustomAttributes(false);

        // Should have AutomaticRetry attribute
        var retryAttribute = attributes
            .FirstOrDefault(a => a.GetType().Name == "AutomaticRetryAttribute");
        retryAttribute.Should().NotBeNull("ProcessMessagesIndefinitely should have AutomaticRetry attribute");

        // Should have Queue attribute
        var queueAttribute = attributes
            .FirstOrDefault(a => a.GetType().Name == "QueueAttribute");
        queueAttribute.Should().NotBeNull("ProcessMessagesIndefinitely should have Queue attribute");
    }

    [Fact]
    public void Job_AutomaticRetryAttribute_ShouldHaveCorrectConfiguration()
    {
        // Arrange
        var method = typeof(UserCreatedEventConsumerJob)
            .GetMethod("ProcessMessagesIndefinitely");

        // Act
        var retryAttribute = method!
            .GetCustomAttributes(false)
            .FirstOrDefault(a => a.GetType().Name == "AutomaticRetryAttribute");

        // Assert
        retryAttribute.Should().NotBeNull();

        var attemptsProperty = retryAttribute!.GetType().GetProperty("Attempts");
        attemptsProperty.Should().NotBeNull();

        var attempts = attemptsProperty!.GetValue(retryAttribute);
        attempts.Should().Be(int.MaxValue, "Job should retry indefinitely");

        var delaysProperty = retryAttribute.GetType().GetProperty("DelaysInSeconds");
        delaysProperty.Should().NotBeNull();

        var delays = delaysProperty!.GetValue(retryAttribute) as int[];
        delays.Should().NotBeNull();
        delays.Should().Contain(10, "Job should wait 10 seconds between retries");
    }

    [Fact]
    public void Job_QueueAttribute_ShouldUseKafkaConsumersQueue()
    {
        // Arrange
        var method = typeof(UserCreatedEventConsumerJob)
            .GetMethod("ProcessMessagesIndefinitely");

        // Act
        var queueAttribute = method!
            .GetCustomAttributes(false)
            .FirstOrDefault(a => a.GetType().Name == "QueueAttribute");

        // Assert
        queueAttribute.Should().NotBeNull();

        var queueNameProperty = queueAttribute!.GetType().GetProperty("Queue");
        queueNameProperty.Should().NotBeNull();

        var queueName = queueNameProperty!.GetValue(queueAttribute) as string;
        queueName.Should().Be("kafka-consumers", "Job should use kafka-consumers queue");
    }

    [Fact]
    public void Job_ProcessMessagesIndefinitely_ShouldAcceptCancellationToken()
    {
        // Arrange
        var method = typeof(UserCreatedEventConsumerJob)
            .GetMethod("ProcessMessagesIndefinitely");

        // Act
        var parameters = method!.GetParameters();

        // Assert
        parameters.Should().HaveCount(1);
        parameters[0].ParameterType.Should().Be(typeof(CancellationToken));
        parameters[0].Name.Should().Be("cancellationToken");
    }

    [Fact]
    public void Job_ProcessMessagesIndefinitely_ShouldReturnTask()
    {
        // Arrange
        var method = typeof(UserCreatedEventConsumerJob)
            .GetMethod("ProcessMessagesIndefinitely");

        // Act & Assert
        method!.ReturnType.Should().Be(typeof(Task));
    }

    [Fact]
    public void Job_ShouldHaveDisposeMethod()
    {
        // Arrange & Act
        var method = typeof(UserCreatedEventConsumerJob)
            .GetMethod("Dispose");

        // Assert
        method.Should().NotBeNull("Job should have Dispose method for cleanup");
        method!.ReturnType.Should().Be(typeof(void));
    }
}