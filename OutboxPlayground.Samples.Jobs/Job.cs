
using Confluent.Kafka;
using OutboxPlayground.Infra.Abstractions;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace OutboxPlayground.Samples.Jobs;

internal class Job : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConsumerConfig _kafkaConfig;
    private readonly string TOPIC_NAME = "sample-topic"; // TODO: from config

    public Job(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
        _kafkaConfig = GetKafkaConfig(TOPIC_NAME);

    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<string, string>(_kafkaConfig).Build();
        consumer.Subscribe(TOPIC_NAME);

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

    private static ConsumerConfig GetKafkaConfig(string TOPIC_NAME)
    {
        ConsumerConfig kafkaConfig = new ConsumerConfig
        {
            Acks = Acks.All, // Ensure all messages are acknowledged
            BootstrapServers = "localhost:9092", // TODO: from config
            GroupId = $"test-group-{TOPIC_NAME}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false, // Disable auto commit to control offsets manually
            EnablePartitionEof = true
        };
        return kafkaConfig;
    }

    #endregion //  GetKafkaConfig
}
