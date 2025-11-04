using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OutboxPlayground.Infra.Abstractions;
using OutboxPlayground.Samples.Abstractions;
using System.Text.Json;

namespace OutboxPlayground.Samples.EFRepository.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for outbox integration tests providing common setup and utilities
/// </summary>
public abstract class OutboxIntegrationTestBase : IClassFixture<TestContainerFixture>, IAsyncLifetime
{
    protected readonly TestContainerFixture _fixture;
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ILogger _logger;

    protected OutboxIntegrationTestBase(TestContainerFixture fixture)
    {
        _fixture = fixture;
        
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        
        _logger = _serviceProvider.GetRequiredService<ILogger<OutboxIntegrationTestBase>>();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // JSON Schema Provider
        services.AddJsonDataSchemaProvider();

        // Risk Assessment Service
        services.AddSingleton<IRiskAssessmentService, TestRiskAssessmentService>();

        // Payment Repository with test connection string
        services.AddPaymentRepository(_fixture.SqlServerConnectionString);

        // HTTP Client for Kafka Connect API
        services.AddHttpClient();
    }

    protected async Task EnsureDatabaseCreatedAsync()
    {
        // Ensure databases are created for both contexts
        using var scope = _serviceProvider.CreateScope();
        
        var singleOutboxContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PaymentDbContext>>();
        using var singleContext = await singleOutboxContextFactory.CreateDbContextAsync();
        await singleContext.Database.EnsureCreatedAsync();

        var multiOutboxContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<PaymentDbMultiOutboxContext>>();
        using var multiContext = await multiOutboxContextFactory.CreateDbContextAsync();
        await multiContext.Database.EnsureCreatedAsync();
    }

    protected async Task<List<ConsumeResult<string, byte[]>>> ConsumeKafkaMessagesAsync(
        string topic, 
        TimeSpan timeout, 
        int expectedMessageCount = 1)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _fixture.KafkaBootstrapServers,
            GroupId = $"test-group-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        consumer.Subscribe(topic);

        var messages = new List<ConsumeResult<string, byte[]>>();
        var cancellationTokenSource = new CancellationTokenSource(timeout);

        try
        {
            while (messages.Count < expectedMessageCount && !cancellationTokenSource.Token.IsCancellationRequested)
            {
                var result = consumer.Consume(cancellationTokenSource.Token);
                if (result?.Message != null)
                {
                    messages.Add(result);
                    _logger.LogInformation("Consumed message from topic {Topic}: Key={Key}", topic, result.Message.Key);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Timeout waiting for messages from topic {Topic}. Expected: {Expected}, Actual: {Actual}", 
                topic, expectedMessageCount, messages.Count);
        }

        return messages;
    }

    protected PaymentRequest CreateTestPaymentRequest(Risk? forceRisk = null)
    {
        return new PaymentRequest(
            Id: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            UserName: "John Doe",
            Amount: 100.50m,
            Currency: "USD",
            PaymentMethod: "CreditCard",
            CustomerId: Guid.NewGuid(),
            CreatedAt: DateTime.UtcNow,
            Status: PaymentStatus.Pending
        );
    }

    protected void ValidateCloudEventHeaders(Headers headers, PaymentMessage expectedMessage)
    {
        // Helper method to get header value
        string? GetHeaderValue(string key) => 
            headers.FirstOrDefault(h => h.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                   ?.GetValueBytes() is byte[] bytes ? System.Text.Encoding.UTF8.GetString(bytes) : null;

        // Validate CloudEvent headers
        Assert.NotNull(GetHeaderValue("ce_id"));
        Assert.Equal("PaymentCreated", GetHeaderValue("ce_type"));
        Assert.Equal("MyBusinessDomain", GetHeaderValue("ce_source"));
        Assert.Equal("application/json", GetHeaderValue("content_type"));
        Assert.NotNull(GetHeaderValue("ce_time"));
        
        // Validate trace parent header exists
        var traceParent = GetHeaderValue("cp_traceparent");
        Assert.NotNull(traceParent);
        Assert.NotEmpty(traceParent);
    }

    protected PaymentMessage DeserializeMessage(byte[] messageData)
    {
        var json = System.Text.Encoding.UTF8.GetString(messageData);
        var message = JsonSerializer.Deserialize<PaymentMessage>(json);
        Assert.NotNull(message);
        return message;
    }

    public virtual Task InitializeAsync() => EnsureDatabaseCreatedAsync();

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected class TestRiskAssessmentService : IRiskAssessmentService
    {
        private Risk? _forceRisk;

        public void ForceRisk(Risk risk) => _forceRisk = risk;

        public Task<Risk> AssessRiskAsync(PaymentRequest payment, CancellationToken cancellationToken = default)
        {
            if (_forceRisk.HasValue)
                return Task.FromResult(_forceRisk.Value);

            // Simple test logic: High risk for amounts > 1000, Medium for > 100, Low otherwise
            return Task.FromResult(payment.Amount switch
            {
                > 1000 => Risk.High,
                > 100 => Risk.Medium,
                _ => Risk.Low
            });
        }
    }
}