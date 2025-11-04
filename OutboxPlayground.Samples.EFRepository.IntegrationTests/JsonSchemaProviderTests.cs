using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OutboxPlayground.Infra.Abstractions;
using OutboxPlayground.Samples.Abstractions;
using OutboxPlayground.Samples.EFRepository.IntegrationTests.Infrastructure;
using System.Text.Json;

namespace OutboxPlayground.Samples.EFRepository.IntegrationTests;

/// <summary>
/// Tests specifically for JSON schema provider functionality
/// </summary>
public class JsonSchemaProviderTests : OutboxIntegrationTestBase
{
    public JsonSchemaProviderTests(TestContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public void JsonSchemaProvider_ShouldHaveCorrectConfiguration()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var schemaProvider = scope.ServiceProvider.GetRequiredService<IDataSchemaProvider>();

        // Assert
        schemaProvider.DataContentType.Should().Be("application/json");
        schemaProvider.SupportsValidation.Should().BeFalse(); // JSON provider typically doesn't support validation
    }

    [Fact]
    public void JsonSchemaProvider_SerializePaymentMessage_ShouldProduceValidJson()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var schemaProvider = scope.ServiceProvider.GetRequiredService<IDataSchemaProvider>();
        
        var paymentMessage = new PaymentMessage(
            Id: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Amount: 123.45m,
            Currency: "USD",
            PaymentMethod: "CreditCard",
            CustomerId: Guid.NewGuid(),
            CreatedAt: DateTime.UtcNow,
            Status: PaymentStatus.Completed,
            RiskAssessment: Risk.Medium
        );

        // Act
        var serializedData = schemaProvider.Serialize(paymentMessage);

        // Assert
        serializedData.Should().NotBeNull();
        serializedData.Should().NotBeEmpty();

        // Verify it's valid JSON by deserializing
        var json = System.Text.Encoding.UTF8.GetString(serializedData);
        var deserializedMessage = JsonSerializer.Deserialize<PaymentMessage>(json);
        
        deserializedMessage.Should().NotBeNull();
        deserializedMessage!.Id.Should().Be(paymentMessage.Id);
        deserializedMessage.Amount.Should().Be(paymentMessage.Amount);
        deserializedMessage.Currency.Should().Be(paymentMessage.Currency);
        deserializedMessage.RiskAssessment.Should().Be(paymentMessage.RiskAssessment);
    }

    [Fact]
    public void JsonSchemaProvider_SerializeComplexData_ShouldHandleSpecialCharacters()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var schemaProvider = scope.ServiceProvider.GetRequiredService<IDataSchemaProvider>();
        
        var paymentMessage = new PaymentMessage(
            Id: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            Amount: 999.999m,
            Currency: "А EUR with special chars: едц!@#$%^&*()",
            PaymentMethod: "\"Bank Transfer\" with quotes",
            CustomerId: Guid.NewGuid(),
            CreatedAt: DateTime.UtcNow,
            Status: PaymentStatus.Failed,
            RiskAssessment: Risk.High
        );

        // Act
        var serializedData = schemaProvider.Serialize(paymentMessage);

        // Assert
        var json = System.Text.Encoding.UTF8.GetString(serializedData);
        var deserializedMessage = JsonSerializer.Deserialize<PaymentMessage>(json);
        
        deserializedMessage.Should().NotBeNull();
        deserializedMessage!.Currency.Should().Be(paymentMessage.Currency);
        deserializedMessage.PaymentMethod.Should().Be(paymentMessage.PaymentMethod);
    }

    [Fact]
    public async Task JsonSchemaProvider_InOutboxFlow_ShouldPreserveDataTypes()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IPaymentRepository>("default");
        var riskService = (TestRiskAssessmentService)scope.ServiceProvider.GetRequiredService<IRiskAssessmentService>();
        
        riskService.ForceRisk(Risk.Medium);
        
        var payment = new PaymentRequest(
            Id: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            UserName: "Test User",
            Amount: 12345.6789m, // Test decimal precision
            Currency: "USD",
            PaymentMethod: "PayPal",
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
        
        // Verify all data types are preserved correctly
        deserializedMessage.Id.Should().Be(payment.Id);
        deserializedMessage.UserId.Should().Be(payment.UserId);
        deserializedMessage.Amount.Should().Be(payment.Amount); // Decimal precision preserved
        deserializedMessage.Currency.Should().Be(payment.Currency);
        deserializedMessage.PaymentMethod.Should().Be(payment.PaymentMethod);
        deserializedMessage.CustomerId.Should().Be(payment.CustomerId);
        deserializedMessage.Status.Should().Be(payment.Status);
        deserializedMessage.RiskAssessment.Should().Be(Risk.Medium);
        
        // Verify DateTime is close (allowing for small serialization differences)
        deserializedMessage.CreatedAt.Should().BeCloseTo(payment.CreatedAt, TimeSpan.FromSeconds(1));
    }

    public override Task InitializeAsync() => base.InitializeAsync();

    public override Task DisposeAsync() => base.DisposeAsync();
}