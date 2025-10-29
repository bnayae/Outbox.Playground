using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OutboxPlayground.Samples.Abstractions;
using OutboxPlayground.Samples.EFRepository.IntegrationTests.Infrastructure;
using System.Text.Json;

namespace OutboxPlayground.Samples.EFRepository.IntegrationTests;

/// <summary>
/// Integration tests for single outbox table scenario using JSON schema provider
/// </summary>
public class SingleOutboxIntegrationTests : OutboxIntegrationTestBase
{
    public SingleOutboxIntegrationTests(TestContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AddPaymentAsync_WithJsonSchemaProvider_ShouldPublishMessageToKafka()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IPaymentRepository>("default");
        var payment = CreateTestPaymentRequest();

        // Act
        await repository.AddPaymentAsync(payment);

        // Allow some time for the outbox processor to work
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert
        var messages = await ConsumeKafkaMessagesAsync("applicationevents_Outbox", TimeSpan.FromSeconds(10));

        messages.Should().HaveCount(1);
        var kafkaMessage = messages.First();

        // Validate message headers
        ValidateCloudEventHeaders(kafkaMessage.Message.Headers, null!);

        // Validate message content
        var deserializedMessage = DeserializeMessage(kafkaMessage.Message.Value);
        deserializedMessage.Id.Should().Be(payment.Id);
        deserializedMessage.UserId.Should().Be(payment.UserId);
        deserializedMessage.Amount.Should().Be(payment.Amount);
        deserializedMessage.Currency.Should().Be(payment.Currency);
        deserializedMessage.PaymentMethod.Should().Be(payment.PaymentMethod);
        deserializedMessage.CustomerId.Should().Be(payment.CustomerId);
        deserializedMessage.Status.Should().Be(payment.Status);
    }

    [Fact]
    public async Task AddPaymentAsync_WithTraceContext_ShouldPreserveTraceParentInHeaders()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IPaymentRepository>("default");
        var payment = CreateTestPaymentRequest();

        // Act
        await repository.AddPaymentAsync(payment);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert
        var messages = await ConsumeKafkaMessagesAsync("applicationevents_Outbox", TimeSpan.FromSeconds(10));
        
        messages.Should().HaveCount(1);
        var kafkaMessage = messages.First();

        // Validate trace context is preserved
        var traceParentHeader = kafkaMessage.Message.Headers
            .FirstOrDefault(h => h.Key.Equals("cp_traceparent", StringComparison.OrdinalIgnoreCase));
        
        traceParentHeader.Should().NotBeNull();
        var traceParentValue = System.Text.Encoding.UTF8.GetString(traceParentHeader!.GetValueBytes());
        traceParentValue.Should().NotBeNullOrEmpty();
        
        // Trace parent should follow W3C format: 00-{trace-id}-{span-id}-{flags}
        traceParentValue.Should().MatchRegex(@"^00-[a-f0-9]{32}-[a-f0-9]{16}-[0-9]{2}$");
    }

    [Theory]
    [InlineData(Risk.Low)]
    [InlineData(Risk.Medium)] 
    [InlineData(Risk.High)]
    public async Task AddPaymentAsync_WithDifferentRiskLevels_ShouldIncludeRiskInMessage(Risk expectedRisk)
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IPaymentRepository>("default");
        var riskService = (TestRiskAssessmentService)scope.ServiceProvider.GetRequiredService<IRiskAssessmentService>();
        
        riskService.ForceRisk(expectedRisk);
        var payment = CreateTestPaymentRequest();

        // Act
        await repository.AddPaymentAsync(payment);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert
        var messages = await ConsumeKafkaMessagesAsync("applicationevents_Outbox", TimeSpan.FromSeconds(10));
        
        messages.Should().HaveCount(1);
        var deserializedMessage = DeserializeMessage(messages.First().Message.Value);
        deserializedMessage.RiskAssessment.Should().Be(expectedRisk);
    }

    [Fact]
    public async Task AddPaymentAsync_ShouldHaveCorrectCloudEventMetadata()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IPaymentRepository>("default");
        var payment = CreateTestPaymentRequest();

        // Act
        await repository.AddPaymentAsync(payment);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert
        var messages = await ConsumeKafkaMessagesAsync("applicationevents_Outbox", TimeSpan.FromSeconds(10));
        
        messages.Should().HaveCount(1);
        var headers = messages.First().Message.Headers;

        // Validate CloudEvent specification compliance
        GetHeaderValue(headers, "ce_specversion").Should().Be("1.0");
        GetHeaderValue(headers, "ce_type").Should().Be("PaymentCreated");
        GetHeaderValue(headers, "ce_source").Should().Be("MyBusinessDomain");
        GetHeaderValue(headers, "content_type").Should().Be("application/json");
        
        // Validate time format (should be ISO 8601)
        var timeHeader = GetHeaderValue(headers, "ce_time");
        timeHeader.Should().NotBeNullOrEmpty();
        DateTime.TryParse(timeHeader, out _).Should().BeTrue();
    }

    [Fact]
    public async Task AddPaymentAsync_WithJsonSerialization_ShouldPreserveDataIntegrity()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IPaymentRepository>("default");
        
        var payment = new PaymentRequest(
            Id: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            UserName: "Test User with Special Characters: едц!@#$%^&*()",
            Amount: 123.456789m, // Test decimal precision
            Currency: "SEK",
            PaymentMethod: "Bank Transfer",
            CustomerId: Guid.NewGuid(),
            CreatedAt: DateTime.UtcNow,
            Status: PaymentStatus.Processing
        );

        // Act
        await repository.AddPaymentAsync(payment);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert
        var messages = await ConsumeKafkaMessagesAsync("applicationevents_Outbox", TimeSpan.FromSeconds(10));
        
        messages.Should().HaveCount(1);
        var deserializedMessage = DeserializeMessage(messages.First().Message.Value);
        
        // Validate all fields are preserved exactly
        deserializedMessage.Id.Should().Be(payment.Id);
        deserializedMessage.UserId.Should().Be(payment.UserId);
        deserializedMessage.Amount.Should().Be(payment.Amount);
        deserializedMessage.Currency.Should().Be(payment.Currency);
        deserializedMessage.PaymentMethod.Should().Be(payment.PaymentMethod);
        deserializedMessage.CustomerId.Should().Be(payment.CustomerId);
        deserializedMessage.CreatedAt.Should().BeCloseTo(payment.CreatedAt, TimeSpan.FromSeconds(1));
        deserializedMessage.Status.Should().Be(payment.Status);
    }

    private static string? GetHeaderValue(Confluent.Kafka.Headers headers, string key) =>
        headers.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
               ?.GetValueBytes() is byte[] bytes ? System.Text.Encoding.UTF8.GetString(bytes) : null;
}