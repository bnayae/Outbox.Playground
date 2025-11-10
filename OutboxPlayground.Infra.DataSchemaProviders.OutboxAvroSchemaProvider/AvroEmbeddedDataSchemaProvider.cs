using Avro;
using Avro.Specific;
using Microsoft.Extensions.Logging;
using OutboxPlayground.Infra.Abstractions;
#pragma warning disable VSSpell001 // Spell Check

namespace OutboxPlayground.Infra.DataSchemaProviders.OutboxAvroSchemaProvider;

/// <summary>
/// Minimal Avro data schema provider using GenericRecord and Apache Avro's native JSON support.
/// </summary>
internal sealed class AvroEmbeddedDataSchemaProvider : IDataSchemaProvider
{
    internal const string DATA_CONTENT_TYPE = "application/avro-embedded";

    //private readonly ISchemaRegistryClient _schemaRegistry;
    private readonly string _dataSchemaPrefix;
    private readonly ILogger<AvroEmbeddedDataSchemaProvider> _logger;

    //public AvroDataSchemaProvider(ISchemaRegistryClient schemaRegistry, ILogger<AvroDataSchemaProvider> logger)
    //{
    //    _schemaRegistry = schemaRegistry;
    //    _logger = logger;
    //}

    public AvroEmbeddedDataSchemaProvider(ILogger<AvroEmbeddedDataSchemaProvider> logger) : this(string.Empty, logger)
    {
    }

    public AvroEmbeddedDataSchemaProvider(string dataSchemaPrefix, ILogger<AvroEmbeddedDataSchemaProvider> logger)
    {
        _dataSchemaPrefix = dataSchemaPrefix;
        _logger = logger;
    }

    string? IDataSchemaProvider.DataContentType { get; } = DATA_CONTENT_TYPE;

    string? IDataSchemaProvider.DataSchemaPrefix => _dataSchemaPrefix;

    bool IDataSchemaProvider.SupportsValidation { get; }

    Task<byte[]> IDataSchemaProvider.SerializeAsync<TData>(TData data)
    {
        if (data is ISpecificRecord specificRecord)
        {
            // Serialize using Avro's native JSON support for SpecificRecord

            byte[] result = specificRecord.SerializeISpecificRecord();
            return Task.FromResult(result);
        }
        else
        {
            throw new InvalidOperationException("Data must implement ISpecificRecord for Avro serialization.");
        }
    }

    Task<bool> IDataSchemaProvider.ValidateAsync<TData>(TData data, string type, string? dataSchema)
    {
        return Task.FromResult(true);
    }
}