using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;
using Testcontainers.Kafka;
using Testcontainers.MsSql;

namespace OutboxPlayground.Samples.EFRepository.IntegrationTests.Infrastructure;

/// <summary>
/// Test fixture that provides containerized infrastructure for integration tests
/// </summary>
public class TestContainerFixture : IAsyncLifetime
{
    private readonly ILogger<TestContainerFixture> _logger;

    public TestContainerFixture()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<TestContainerFixture>();
    }

    public MsSqlContainer SqlServerContainer { get; private set; } = null!;
    public KafkaContainer KafkaContainer { get; private set; } = null!;
    public IContainer SchemaRegistryContainer { get; private set; } = null!;
    public IContainer KafkaConnectContainer { get; private set; } = null!;

    public string SqlServerConnectionString => SqlServerContainer.GetConnectionString();
    public string KafkaBootstrapServers => KafkaContainer.GetBootstrapAddress();
    public string SchemaRegistryUrl => $"http://localhost:{SchemaRegistryContainer.GetMappedPublicPort(8081)}";
    public string KafkaConnectUrl => $"http://localhost:{KafkaConnectContainer.GetMappedPublicPort(8083)}";

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Starting test containers...");

        // Start SQL Server container
        SqlServerContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("MasadNetunim12!@")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithPortBinding(1433, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("/opt/mssql-tools18/bin/sqlcmd", "-S", "localhost", "-U", "sa", "-P", "MasadNetunim12!@", "-C", "-Q", "SELECT 1"))
            .Build();

        await SqlServerContainer.StartAsync();
        _logger.LogInformation("SQL Server container started. Connection: {ConnectionString}", SqlServerConnectionString);

        // Start Kafka container
        KafkaContainer = new KafkaBuilder()
            .WithImage("confluentinc/cp-kafka:7.8.0")
            .WithPortBinding(9092, true)
            .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
            .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1")
            .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
            .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1")
            .Build();

        await KafkaContainer.StartAsync();
        _logger.LogInformation("Kafka container started. Bootstrap servers: {BootstrapServers}", KafkaBootstrapServers);

        // Start Schema Registry container
        SchemaRegistryContainer = new ContainerBuilder()
            .WithImage("confluentinc/cp-schema-registry:7.8.0")
            .WithPortBinding(8081, true)
            .WithEnvironment("SCHEMA_REGISTRY_HOST_NAME", "schema-registry")
            .WithEnvironment("SCHEMA_REGISTRY_KAFKASTORE_BOOTSTRAP_SERVERS", KafkaBootstrapServers)
            .WithEnvironment("SCHEMA_REGISTRY_LISTENERS", "http://0.0.0.0:8081")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8081).ForPath("/subjects")))
            .Build();

        await SchemaRegistryContainer.StartAsync();
        _logger.LogInformation("Schema Registry container started. URL: {SchemaRegistryUrl}", SchemaRegistryUrl);

        // Start Kafka Connect container with JDBC connector
        KafkaConnectContainer = new ContainerBuilder()
            .WithImage("confluentinc/cp-kafka-connect:7.8.0")
            .WithPortBinding(8083, true)
            .WithEnvironment("CONNECT_BOOTSTRAP_SERVERS", KafkaBootstrapServers)
            .WithEnvironment("CONNECT_REST_ADVERTISED_HOST_NAME", "connect")
            .WithEnvironment("CONNECT_GROUP_ID", "compose-connect-group")
            .WithEnvironment("CONNECT_CONFIG_STORAGE_TOPIC", "docker-connect-configs")
            .WithEnvironment("CONNECT_CONFIG_STORAGE_REPLICATION_FACTOR", "1")
            .WithEnvironment("CONNECT_OFFSET_FLUSH_INTERVAL_MS", "10000")
            .WithEnvironment("CONNECT_OFFSET_STORAGE_TOPIC", "docker-connect-offsets")
            .WithEnvironment("CONNECT_OFFSET_STORAGE_REPLICATION_FACTOR", "1")
            .WithEnvironment("CONNECT_STATUS_STORAGE_TOPIC", "docker-connect-status")
            .WithEnvironment("CONNECT_STATUS_STORAGE_REPLICATION_FACTOR", "1")
            .WithEnvironment("CONNECT_KEY_CONVERTER", "org.apache.kafka.connect.storage.StringConverter")
            .WithEnvironment("CONNECT_VALUE_CONVERTER", "org.apache.kafka.connect.converters.ByteArrayConverter")
            .WithEnvironment("CONNECT_VALUE_CONVERTER_SCHEMAS_ENABLE", "false")
            .WithEnvironment("CONNECT_PLUGIN_PATH", "/usr/share/java,/usr/share/confluent-hub-components")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8083).ForPath("/connectors")))
            .Build();

        await KafkaConnectContainer.StartAsync();
        _logger.LogInformation("Kafka Connect container started. URL: {KafkaConnectUrl}", KafkaConnectUrl);

        _logger.LogInformation("All test containers started successfully");
    }

    public async Task DisposeAsync()
    {
        _logger.LogInformation("Stopping test containers...");

        if (KafkaConnectContainer != null)
            await KafkaConnectContainer.StopAsync();

        if (SchemaRegistryContainer != null)
            await SchemaRegistryContainer.StopAsync();

        if (KafkaContainer != null)
            await KafkaContainer.StopAsync();

        if (SqlServerContainer != null)
            await SqlServerContainer.StopAsync();

        _logger.LogInformation("All test containers stopped");
    }
}