using Chr.Avro.Abstract;
using Chr.Avro.Serialization;
using Microsoft.Extensions.Logging;
using OutboxPlayground.Infra.Abstractions;

namespace OutboxPlayground.Infra.DataSchemaProviders.OutboxAvroSchemaProvider;

/// <summary>
/// Provides Avro data schema functionalities including validation and serialization.
/// </summary>
internal sealed class AvroDataSchemaProvider : IDataSchemaProvider
{
    private readonly ILogger<AvroDataSchemaProvider> _logger;
    private readonly SchemaBuilder _schemaBuilder;
    private readonly BinarySerializerBuilder _serializerBuilder;

    public AvroDataSchemaProvider(ILogger<AvroDataSchemaProvider> logger)
    {
        _logger = logger;
        _schemaBuilder = new SchemaBuilder();
        _serializerBuilder = new BinarySerializerBuilder();
    }

    /// <summary>
    /// Gets the content type for Avro data format.
    /// </summary>
    string? IDataSchemaProvider.DataContentType { get; } = "application/avro";

    /// <summary>
    /// Gets the schema prefix for Avro schemas.
    /// </summary>
    string? IDataSchemaProvider.DataSchemaPrefix { get; } = "urn://avro-schema-registry/";

    /// <summary>
    /// Validates the provided data against the schema.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <param name="data"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    Task<bool> IDataSchemaProvider.ValidateAsync<TData>(TData data, string type)
    {
        if (data == null || string.IsNullOrEmpty(type))
            return Task.FromResult(false);

        try
        {
            // Validate by attempting serialization
            _ = SerializeToAvro(data);
            _logger.LogDebug("Validation succeeded for type {TypeName}", typeof(TData).Name);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Validation failed for type {TypeName}: {Message}",
                typeof(TData).Name, ex.Message);
            return Task.FromResult(false);
        }
    }

    /// <summary>
    /// Serializes the provided data to Avro binary format.
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    byte[] IDataSchemaProvider.Serialize<TData>(TData data) => EqualityComparer<TData>.Default.Equals(data, default)
        ? throw new ArgumentNullException(nameof(data))
        : SerializeToAvro(data);

    private byte[] SerializeToAvro<TData>(TData data)
    {
        try
        {
            // Build schema from type
            var schema = _schemaBuilder.BuildSchema<TData>();

            // Build serializer using the correct method name
            var serializer = _serializerBuilder.BuildDelegate<TData>(schema);

            // Serialize to binary
            using var stream = new MemoryStream();
            using var writer = new Chr.Avro.Serialization.BinaryWriter(stream);
            serializer(data, writer);

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

}