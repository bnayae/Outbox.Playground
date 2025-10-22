using Confluent.Kafka;
using OutboxPlayground.Infra.Abstractions;

namespace OutboxPlayground.Samples.Jobs;

internal class Job : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConsumerConfig _kafkaConfig;
    private readonly string _topicName;

    public Job(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _topicName = configuration["Kafka:TopicName"] ?? throw new ArgumentException("Kafka:TopicName configuration is required");
        _kafkaConfig = GetKafkaConfig(configuration);
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<string, string>(_kafkaConfig).Build();
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
                var value = cr?.Message?.Value;
                if (value == null)
                {
                    continue; // or log and skip
                }

                #endregion //  Validation

                var record = System.Text.Json.JsonSerializer.Deserialize<CloudEvent>(value);

                // TODO: Activity + traceparent + log
                await Task.Yield(); // Simulate async work

                // Commit offset synchronously
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
