using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OutboxPlayground.Samples.Abstractions;
using OutboxPlayground.Samples.EFRepository.IntegrationTests.Infrastructure;

namespace OutboxPlayground.Samples.EFRepository.IntegrationTests;

/// <summary>
/// Integration tests for multiple outbox tables scenario using JSON schema provider
/// </summary>
public class MultiOutboxIntegrationTests : OutboxIntegrationTestBase
{
    public MultiOutboxIntegrationTests(TestContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task AddPaymentAsync_WithLowRisk_ShouldPublishToMainOutboxOnly()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IPaymentRepository>("multi-outbox");
        var riskService = (TestRiskAssessmentService)scope.ServiceProvider.GetRequiredService<IRiskAssessmentService>();
        
        riskService.ForceRisk(Risk.Low);
        var payment = CreateTestPaymentRequest();

        // Act
        await repository.AddPaymentAsync(payment);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert - Should have message in main outbox topic
        var mainMessages = await ConsumeKafkaMessagesAsync("applicationevents_Outbox", TimeSpan.FromSeconds(5));
        mainMessages.Should().HaveCount(1);

        // Assert - Should NOT have message in high risk outbox topic
        var highRiskMessages = await ConsumeKafkaMessagesAsync("applicationevents_HighRiskOutbox", TimeSpan.FromSeconds(2));
        highRiskMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task AddPaymentAsync_WithHighRisk_ShouldPublishToBothOutboxes()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IPaymentRepository>("multi-outbox");
        var riskService = (TestRiskAssessmentService)scope.ServiceProvider.GetRequiredService<IRiskAssessmentService>();
        
        riskService.ForceRisk(Risk.High);
        var payment = CreateTestPaymentRequest();

        // Act
        await repository.AddPaymentAsync(payment);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert - Should have message in main outbox topic
        var mainMessages = await ConsumeKafkaMessagesAsync("applicationevents_Outbox", TimeSpan.FromSeconds(5));
        mainMessages.Should().HaveCount(1);

        // Assert - Should ALSO have message in high risk outbox topic
        var highRiskMessages = await ConsumeKafkaMessagesAsync("applicationevents_HighRiskOutbox", TimeSpan.FromSeconds(5));
        highRiskMessages.Should().HaveCount(1);

        // Validate both messages contain the same payment data
        var mainMessage = DeserializeMessage(mainMessages.First().Message.Value);
        var highRiskMessage = DeserializeMessage(highRiskMessages.First().Message.Value);

        mainMessage.Id.Should().Be(highRiskMessage.Id);
        mainMessage.Amount.Should().Be(highRiskMessage.Amount);
        mainMessage.RiskAssessment.Should().Be(Risk.High);
        highRiskMessage.RiskAssessment.Should().Be(Risk.High);
    }

    [Fact]
    public async Task AddPaymentAsync_WithMediumRisk_ShouldPublishToMainOutboxOnly()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IPaymentRepository>("multi-outbox");
        var riskService = (TestRiskAssessmentService)scope.ServiceProvider.GetRequiredService<IRiskAssessmentService>();
        
        riskService.ForceRisk(Risk.Medium);
        var payment = CreateTestPaymentRequest();

        // Act
        await repository.AddPaymentAsync(payment);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert - Should have message in main outbox topic
        var mainMessages = await ConsumeKafkaMessagesAsync("applicationevents_Outbox", TimeSpan.FromSeconds(5));
        mainMessages.Should().HaveCount(1);

        // Assert - Should NOT have message in high risk outbox topic
        var highRiskMessages = await ConsumeKafkaMessagesAsync("applicationevents_HighRiskOutbox", TimeSpan.FromSeconds(2));
        highRiskMessages.Should().BeEmpty();

        // Validate message content
        var message = DeserializeMessage(mainMessages.First().Message.Value);
        message.RiskAssessment.Should().Be(Risk.Medium);
    }

    [Fact]
    public async Task AddPaymentAsync_MultipleHighRiskPayments_ShouldPublishAllToBothOutboxes()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IPaymentRepository>("multi-outbox");
        var riskService = (TestRiskAssessmentService)scope.ServiceProvider.GetRequiredService<IRiskAssessmentService>();
        
        riskService.ForceRisk(Risk.High);
        var payment1 = CreateTestPaymentRequest();
        var payment2 = CreateTestPaymentRequest();
        var payment3 = CreateTestPaymentRequest();

        // Act
        await repository.AddPaymentAsync(payment1);
        await repository.AddPaymentAsync(payment2);
        await repository.AddPaymentAsync(payment3);
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Assert - Should have 3 messages in main outbox topic
        var mainMessages = await ConsumeKafkaMessagesAsync("applicationevents_Outbox", TimeSpan.FromSeconds(5), 3);
        mainMessages.Should().HaveCount(3);

        // Assert - Should ALSO have 3 messages in high risk outbox topic
        var highRiskMessages = await ConsumeKafkaMessagesAsync("applicationevents_HighRiskOutbox", TimeSpan.FromSeconds(5), 3);
        highRiskMessages.Should().HaveCount(3);

        // Validate message IDs match
        var mainIds = mainMessages.Select(m => DeserializeMessage(m.Message.Value).Id).OrderBy(id => id).ToList();
        var highRiskIds = highRiskMessages.Select(m => DeserializeMessage(m.Message.Value).Id).OrderBy(id => id).ToList();
        var expectedIds = new[] { payment1.Id, payment2.Id, payment3.Id }.OrderBy(id => id).ToList();

        mainIds.Should().BeEquivalentTo(expectedIds);
        highRiskIds.Should().BeEquivalentTo(expectedIds);
    }

    [Fact]
    public async Task AddPaymentAsync_HighRiskPayment_ShouldHaveConsistentHeadersInBothTopics()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IPaymentRepository>("multi-outbox");
        var riskService = (TestRiskAssessmentService)scope.ServiceProvider.GetRequiredService<IRiskAssessmentService>();
        
        riskService.ForceRisk(Risk.High);
        var payment = CreateTestPaymentRequest();

        // Act
        await repository.AddPaymentAsync(payment);
        await Task.Delay(TimeSpan.FromSeconds(2));

        // Assert
        var mainMessages = await ConsumeKafkaMessagesAsync("applicationevents_Outbox", TimeSpan.FromSeconds(5));
        var highRiskMessages = await ConsumeKafkaMessagesAsync("applicationevents_HighRiskOutbox", TimeSpan.FromSeconds(5));

        mainMessages.Should().HaveCount(1);
        highRiskMessages.Should().HaveCount(1);

        // Validate headers are consistent between both topics
        var mainHeaders = mainMessages.First().Message.Headers;
        var highRiskHeaders = highRiskMessages.First().Message.Headers;

        ValidateCloudEventHeaders(mainHeaders, null!);
        ValidateCloudEventHeaders(highRiskHeaders, null!);

        // Both should have the same CloudEvent metadata
        GetHeaderValue(mainHeaders, "ce_type").Should().Be(GetHeaderValue(highRiskHeaders, "ce_type"));
        GetHeaderValue(mainHeaders, "ce_source").Should().Be(GetHeaderValue(highRiskHeaders, "ce_source"));
        GetHeaderValue(mainHeaders, "content_type").Should().Be(GetHeaderValue(highRiskHeaders, "content_type"));
    }

    private static string? GetHeaderValue(Confluent.Kafka.Headers headers, string key) =>
        headers.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
               ?.GetValueBytes() is byte[] bytes ? System.Text.Encoding.UTF8.GetString(bytes) : null;
}