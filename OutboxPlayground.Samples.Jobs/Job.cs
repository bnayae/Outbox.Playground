using Confluent.Kafka;
using OutboxPlayground.Infra.Abstractions;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions;
using System.Text.Json;
using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.Jobs;

internal class Job : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConsumerConfig _kafkaConfig;
    private readonly string _topicName;

    public Job(ILogger<Job> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _topicName = configuration["Kafka:TopicName"] ?? throw new ArgumentException("Kafka:TopicName configuration is required");
        _kafkaConfig = GetKafkaConfig(configuration);
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<string, byte[]>(_kafkaConfig).Build();
        consumer.Subscribe(_topicName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var cr = consumer.Consume(stoppingToken);

                #region Validation

                if (cr.IsPartitionEOF)
                {
                    // End of partition reached, continue to next partition
                    continue;
                }
                byte[]? value = cr?.Message?.Value;
                if (value == null)
                {
                    continue; // or log and skip
                }

                #endregion //  Validation

                // Get headers from message for distributed tracing and CloudEvent metadata

                #region Extract Headers

                OtelTraceParent? traceParent = null;
                string? ceType = null;
                string? ceTime = null;
                string? contentType = null;

                if (cr.Message.Headers != null)
                {
                    string? GetHeader(string headerName)
                    {
                        var header = cr.Message.Headers.FirstOrDefault(h => 
                            string.Equals(h.Key, headerName, StringComparison.OrdinalIgnoreCase));
                        return header != null ? System.Text.Encoding.UTF8.GetString(header.GetValueBytes()) : null;
                    }

                    traceParent = GetHeader( "cp_traceparent");
                    ceType = GetHeader( "ce_type");
                    ceTime = GetHeader("ce_time");
                    contentType = GetHeader("content_type");
                }

                #endregion //  Extract Headers

                using var scp = _scopeFactory.CreateScope();
                using var activity = OtelExtensions.ACTIVITY_SOURCE.StartActivity("ProcessKafkaMessage");
                ActivityContext activityContxt = traceParent.ToTelemetryContext();
                if( activityContxt != default)
                {
                    activity?.AddLink(new ActivityLink(activityContxt));
                }

                var payment = JsonSerializer.Deserialize<PaymentMessage>(value);

                _logger.LogProcessingMessage(ceType, ceTime, contentType);


                await Task.Yield(); // Simulate async work

                consumer.Commit(cr);
            }
        }
        catch (OperationCanceledException) { /* Exit cleanly */ }
        finally
        {
            consumer.Close();
        }
    }


    #region GetKafkaConfig

    private ConsumerConfig GetKafkaConfig(IConfiguration configuration)
    {
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? throw new ArgumentException("Kafka:BootstrapServers configuration is required");

        ConsumerConfig kafkaConfig = new ConsumerConfig
        {
            Acks = Acks.All, // Ensure all messages are acknowledged
            BootstrapServers = bootstrapServers,
            GroupId = $"test-group-{_topicName}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // Disable auto commit to control offsets manually
            EnablePartitionEof = true
        };
        return kafkaConfig;
    }

    #endregion //  GetKafkaConfig
}
