using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OutboxPlayground.Samples.Abstractions;
using OutboxPlayground.Samples.EFRepository.IntegrationTests.Infrastructure;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace OutboxPlayground.Samples.EFRepository.IntegrationTests;

/// <summary>
/// Integration tests that verify the full end-to-end outbox pattern with Kafka Connector
/// </summary>
public class KafkaConnectorIntegrationTests : OutboxIntegrationTestBase
{
    public KafkaConnectorIntegrationTests(TestContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task OutboxToKafka_EndToEndFlow_ShouldWorkCorrectly()
    {
        // Arrange
        await SetupKafkaConnectorAsync();
        
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IPaymentRepository>("default");
        var payment = CreateTestPaymentRequest();

        // Act
        await repository.AddPaymentAsync(payment);

        // Wait for connector to process the outbox table
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Assert
        var messages = await ConsumeKafkaMessagesAsync("applicationevents_Outbox", TimeSpan.FromSeconds(15));
        
        messages.Should().HaveCount(1);
        var kafkaMessage = messages.First();

        // Validate the message was processed by the connector correctly
        ValidateCloudEventHeaders(kafkaMessage.Message.Headers, null!);
        var deserializedMessage = DeserializeMessage(kafkaMessage.Message.Value);
        
        deserializedMessage.Id.Should().Be(payment.Id);
        deserializedMessage.Amount.Should().Be(payment.Amount);
    }

    [Fact]
    public async Task MultiOutboxToKafka_HighRiskPayment_ShouldRouteToCorrectTopics()
    {
        // Arrange
        await SetupKafkaConnectorAsync();
        await SetupHighRiskKafkaConnectorAsync();
        
        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredKeyedService<IPaymentRepository>("multi-outbox");
        var riskService = (TestRiskAssessmentService)scope.ServiceProvider.GetRequiredService<IRiskAssessmentService>();
        
        riskService.ForceRisk(Risk.High);
        var payment = CreateTestPaymentRequest();

        // Act
        await repository.AddPaymentAsync(payment);

        // Wait for connectors to process both outbox tables
        await Task.Delay(TimeSpan.FromSeconds(15));

        // Assert
        var mainMessages = await ConsumeKafkaMessagesAsync("applicationevents_Outbox", TimeSpan.FromSeconds(10));
        var highRiskMessages = await ConsumeKafkaMessagesAsync("applicationevents_HighRiskOutbox", TimeSpan.FromSeconds(10));

        mainMessages.Should().HaveCount(1);
        highRiskMessages.Should().HaveCount(1);

        // Both messages should contain the same payment data
        var mainMessage = DeserializeMessage(mainMessages.First().Message.Value);
        var highRiskMessage = DeserializeMessage(highRiskMessages.First().Message.Value);

        mainMessage.Id.Should().Be(payment.Id);
        highRiskMessage.Id.Should().Be(payment.Id);
        mainMessage.RiskAssessment.Should().Be(Risk.High);
        highRiskMessage.RiskAssessment.Should().Be(Risk.High);
    }

    private async Task SetupKafkaConnectorAsync()
    {
        var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
        
        var connectorConfig = new
        {
            name = "mssql-jdbc-source-connector_outbox",
            config = new
            {
                connector_class = "io.confluent.connect.jdbc.JdbcSourceConnector",
                tasks_max = "1",
                errors_log_enable = "true",
                errors_log_include_messages = "true",
                connection_url = _fixture.SqlServerConnectionString,
                connection_user = "sa",
                connection_password = "MasadNetunim12!@",
                table_whitelist = "Outbox",
                numeric_mapping = "none",
                mode = "timestamp",
                timestamp_column_name = "Time",
                timestamp_delay_interval_ms = "0",
                validate_non_null = "false",
                topic_prefix = "applicationevents_",
                topic_creation_default_partitions = "1",
                poll_interval_ms = "5000",
                auto_create_topics_enable = "true",
                topic_creation_default_replication_factor = "1",
                value_converter = "org.apache.kafka.connect.converters.ByteArrayConverter",
                value_converter_schemas_enable = "false",
                transforms = "CreateKey,ExtractKeyField,MoveToHeaders,RemoveFields,ExtractData",
                transforms_CreateKey_type = "org.apache.kafka.connect.transforms.ValueToKey",
                transforms_CreateKey_fields = "Id",
                transforms_ExtractKeyField_type = "org.apache.kafka.connect.transforms.ExtractField$Key",
                transforms_ExtractKeyField_field = "Id",
                transforms_MoveToHeaders_type = "org.apache.kafka.connect.transforms.HeaderFrom$Value",
                transforms_MoveToHeaders_fields = "Id,SpecVersion,Type,Source,Time,DataContentType,Subject,DataRef,TraceParent",
                transforms_MoveToHeaders_headers = "ce_id,ce_specversion,ce_type,ce_source,ce_time,content_type,ce_subject,cp_dataref,cp_traceparent",
                transforms_MoveToHeaders_operation = "move",
                transforms_RemoveFields_type = "org.apache.kafka.connect.transforms.ReplaceField$Value",
                transforms_RemoveFields_blacklist = "Id,SpecVersion,Type,Source,Time,DataContentType,DataSchema,Subject,DataRef,TraceParent",
                transforms_ExtractData_type = "org.apache.kafka.connect.transforms.ExtractField$Value",
                transforms_ExtractData_field = "Data"
            }
        };

        var json = JsonSerializer.Serialize(connectorConfig);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"{_fixture.KafkaConnectUrl}/connectors", content);
        response.EnsureSuccessStatusCode();
    }

    private async Task SetupHighRiskKafkaConnectorAsync()
    {
        var httpClient = _serviceProvider.GetRequiredService<HttpClient>();
        
        var connectorConfig = new
        {
            name = "mssql-jdbc-source-connector_outbox_high_risk",
            config = new
            {
                connector_class = "io.confluent.connect.jdbc.JdbcSourceConnector",
                tasks_max = "1",
                errors_log_enable = "true",
                errors_log_include_messages = "true",
                connection_url = _fixture.SqlServerConnectionString,
                connection_user = "sa",
                connection_password = "MasadNetunim12!@",
                table_whitelist = "HighRiskOutbox",
                numeric_mapping = "none",
                mode = "timestamp",
                timestamp_column_name = "Time",
                timestamp_delay_interval_ms = "0",
                validate_non_null = "false",
                topic_prefix = "applicationevents_",
                topic_creation_default_partitions = "1",
                poll_interval_ms = "5000",
                auto_create_topics_enable = "true",
                topic_creation_default_replication_factor = "1",
                value_converter = "org.apache.kafka.connect.converters.ByteArrayConverter",
                value_converter_schemas_enable = "false",
                transforms = "CreateKey,ExtractKeyField,MoveToHeaders,RemoveFields,ExtractData",
                transforms_CreateKey_type = "org.apache.kafka.connect.transforms.ValueToKey",
                transforms_CreateKey_fields = "Id",
                transforms_ExtractKeyField_type = "org.apache.kafka.connect.transforms.ExtractField$Key",
                transforms_ExtractKeyField_field = "Id",
                transforms_MoveToHeaders_type = "org.apache.kafka.connect.transforms.HeaderFrom$Value",
                transforms_MoveToHeaders_fields = "Id,SpecVersion,Type,Source,Time,DataContentType,Subject,DataRef,TraceParent",
                transforms_MoveToHeaders_headers = "ce_id,ce_specversion,ce_type,ce_source,ce_time,content_type,ce_subject,cp_dataref,cp_traceparent",
                transforms_MoveToHeaders_operation = "move",
                transforms_RemoveFields_type = "org.apache.kafka.connect.transforms.ReplaceField$Value",
                transforms_RemoveFields_blacklist = "Id,SpecVersion,Type,Source,Time,DataContentType,DataSchema,Subject,DataRef,TraceParent",
                transforms_ExtractData_type = "org.apache.kafka.connect.transforms.ExtractField$Value",
                transforms_ExtractData_field = "Data"
            }
        };

        var json = JsonSerializer.Serialize(connectorConfig);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"{_fixture.KafkaConnectUrl}/connectors", content);
        response.EnsureSuccessStatusCode();
    }
}