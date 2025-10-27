using Avro.Generic;
using Avro.IO;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.Logging;
using OutboxPlayground.Infra.Abstractions;
using System.Text;
using System.Text.Json;

namespace OutboxPlayground.Infra.DataSchemaProviders.OutboxAvroSchemaProvider;

/// <summary>
/// Minimal Avro data schema provider using GenericRecord and Apache Avro's native JSON support.
/// </summary>
internal sealed class AvroDataSchemaProvider : IDataSchemaProvider
{
    private readonly ISchemaRegistryClient _schemaRegistry;
    private readonly ILogger<AvroDataSchemaProvider> _logger;

    public AvroDataSchemaProvider(ISchemaRegistryClient schemaRegistry, ILogger<AvroDataSchemaProvider> logger)
    {
        _schemaRegistry = schemaRegistry;
        _logger = logger;
    }

    /// <summary>
    /// Gets the content type for Avro data format.
    /// </summary>
    string? IDataSchemaProvider.DataContentType { get; } = "application/avro";

    /// <summary>
    /// Gets the schema prefix for Avro schemas.
    /// </summary>
    string? IDataSchemaProvider.DataSchemaPrefix { get; } = "urn://avro-schema-registry/";

    bool IDataSchemaProvider.SupportsValidation => throw new NotImplementedException();

    /// <summary>
    /// Validates the provided data against the Avro schema.
    /// </summary>
    /// <typeparam name="TData">The type of data to validate</typeparam>
    /// <param name="data">The data to validate</param>
    /// <param name="type">The data type used as schema suffix</param>
    /// <returns>True if validation passes, false otherwise</returns>
    async Task<bool> IDataSchemaProvider.ValidateAsync<TData>(TData data, string type, string? dataSchema)
    {
        if (data == null || string.IsNullOrEmpty(type)) return false;

        try
        {
            var subject = $"{type}-value";
            var schema = await GetSchemaAsync(subject);
            if (schema == null)
            {
                _logger.SchemaNotFound(subject);
                return false;
            }

            // Validate by converting JSON to GenericRecord without full binary serialization
            return ValidateDataAgainstSchema(data, schema);
        }
        catch (Exception ex)
        {
            _logger.SchemaValidationFailed(type, ex.Message, ex);
            return false;
        }
    }

    /// <summary>
    /// Serializes the provided data into Avro binary format.
    /// </summary>
    /// <typeparam name="TData">The type of data to serialize</typeparam>
    /// <param name="data">The data to serialize</param>
    /// <returns>Serialized data as Avro binary byte array</returns>
    byte[] IDataSchemaProvider.Serialize<TData>(TData data) => data == null
        ? throw new ArgumentNullException(nameof(data))
        : SerializeToAvro(data, GetSchemaAsync($"{typeof(TData).Name}-value").Result
            ?? throw new InvalidOperationException($"Schema not found for {typeof(TData).Name}"));

    /// <summary>
    /// Validates data against schema by converting JSON to GenericRecord.
    /// More efficient than full serialization as it skips the binary encoding step.
    /// </summary>
    private bool ValidateDataAgainstSchema<TData>(TData data, Avro.Schema schema)
    {
        try
        {
            // Convert data to JSON first
            var json = JsonSerializer.Serialize(data);
            using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

            // Use JsonDecoder to validate JSON structure against schema
            var jsonDecoder = new JsonDecoder(schema, jsonStream);
            var reader = new GenericDatumReader<GenericRecord>(schema, schema);

            // This validates the structure by reading JSON into GenericRecord
            // If data doesn't match schema, this will throw an exception
            _ = reader.Read(null, jsonDecoder);

            _logger.SchemaValidationSucceeded(typeof(TData).Name, schema.Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.SchemaValidationDetailsFailed(typeof(TData).Name, schema.Name, ex.Message);
            return false;
        }
    }

    private byte[] SerializeToAvro<TData>(TData data, Avro.Schema schema)
    {
        try
        {
            // JSON -> GenericRecord via Avro's JsonDecoder
            var json = JsonSerializer.Serialize(data);
            using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var jsonDecoder = new JsonDecoder(schema, jsonStream);
            var reader = new GenericDatumReader<GenericRecord>(schema, schema);
            var record = reader.Read(null, jsonDecoder);

            // GenericRecord -> Binary Avro
            using var stream = new MemoryStream();
            var encoder = new BinaryEncoder(stream);
            var writer = new GenericDatumWriter<GenericRecord>(schema);
            writer.Write(record, encoder);
            encoder.Flush();

            var result = stream.ToArray();
            _logger.DataSerialized(typeof(TData).Name, result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.SerializationFailed(typeof(TData).Name, ex.Message, ex);
            throw;
        }
    }

    private async Task<Avro.Schema?> GetSchemaAsync(string subject)
    {
        try
        {
            var response = await _schemaRegistry.GetLatestSchemaAsync(subject);
            var schema = Avro.Schema.Parse(response.SchemaString);
            return schema;
        }
        catch (SchemaRegistryException ex) when (ex.ErrorCode == 40401)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.SchemaRetrievalFailed(subject, ex.Message, ex);
            return null;
        }
    }
}