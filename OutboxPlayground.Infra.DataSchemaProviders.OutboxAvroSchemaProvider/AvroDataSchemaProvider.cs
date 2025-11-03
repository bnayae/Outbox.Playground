using Avro.Generic;
using Avro.IO;
using Confluent.SchemaRegistry;
using Microsoft.Extensions.Logging;
using OutboxPlayground.Infra.Abstractions;
using System.Text;
using System.Text.Json;
#pragma warning disable VSSpell001 // Spell Check

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

    string? IDataSchemaProvider.DataContentType => throw new NotImplementedException();

    string? IDataSchemaProvider.DataSchemaPrefix => throw new NotImplementedException();

    bool IDataSchemaProvider.SupportsValidation => throw new NotImplementedException();

    Task<byte[]> IDataSchemaProvider.SerializeAsync<TData>(TData data)
    {
        throw new NotImplementedException();
    }

    Task<bool> IDataSchemaProvider.ValidateAsync<TData>(TData data, string type, string? dataSchema)
    {
        throw new NotImplementedException();
    }
}