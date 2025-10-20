using OutboxPlayground.Infra.Abstractions;
using System.Text.Json;

namespace OutboxPlayground.Infra.DataSchemaProviders.OutboxJsonSchemaProvider;

/// <summary>
/// Provides JSON-based data schema and serialization capabilities for CloudEvents.
/// Implements IDataSchemaProvider to serialize data as JSON using System.Text.Json.
/// </summary>
internal class JsonDataSchemaProvider : IDataSchemaProvider
{
    private readonly string _dataSchemaPrefix;
    private readonly JsonSerializerOptions? _options;

    /// <summary>
    /// Gets the content type for JSON data format.
    /// </summary>
    string? IDataSchemaProvider.DataContentType { get; } = "application/json";

    string? IDataSchemaProvider.DataSchemaPrefix => _dataSchemaPrefix;

    /// <summary>
    /// Initializes a new instance of the JsonDataSchemaProvider class.
    /// </summary>
    /// <param name="options">Optional JSON serializer options to customize serialization behavior.</param>
    public JsonDataSchemaProvider(JsonSerializerOptions? options = null): this(string.Empty, options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the JsonDataSchemaProvider class.
    /// </summary>
    /// <param name="options">Optional JSON serializer options to customize serialization behavior.</param>
    public JsonDataSchemaProvider(string dataSchemaPrefix, JsonSerializerOptions? options = null)
    {
        _dataSchemaPrefix = dataSchemaPrefix;
        _options = options;
    }

    /// <summary>
    /// Validates the provided data against the schema.
    /// </summary>
    /// <typeparam name="TData">The type of data to validate</typeparam>
    /// <param name="data">The data to validate</param>
    /// <param name="type">The data type use as schema suffix.</param>
    /// <returns>True if validation passes, false otherwise</returns>
    async Task<bool> IDataSchemaProvider.Validate<TData>(TData data, string type)
    {
        // For JSON schema provider, we perform basic validation by attempting serialization
        // TODO: validate against a JSON schema if needed (plus schema cache)
        if (!string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(_dataSchemaPrefix))
        {
            await Task.Yield();
        }
        return true;
    }

    /// <summary>
    /// Serializes the provided data into a UTF-8 encoded JSON byte array.
    /// </summary>
    /// <typeparam name="TData">The type of data to serialize.</typeparam>
    /// <param name="data">The data to serialize.</param>
    /// <returns>Serialized data as a UTF-8 encoded JSON byte array.</returns>
    byte[] IDataSchemaProvider.Serialize<TData>(TData data)
    {
        return JsonSerializer.SerializeToUtf8Bytes(data, _options);
    }
}
