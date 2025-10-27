using System.Text.Json.Serialization;

namespace OutboxPlayground.Infra.Abstractions;

/// <summary>
/// Provides data schema and serialization capabilities for CloudEvents.
/// Enables data to carry any type of content with format and encoding that might differ from the chosen event format.
/// </summary>
public interface IDataSchemaProvider
{
    #region DataContentType

    /// <summary>
    /// Content type of data value. This attribute enables data to carry any type of content, 
    /// whereby format and encoding might differ from that of the chosen event format.
    /// Common examples include: 
    /// - application/json
    /// - application/avro
    /// - application/protobuf
    /// Must follow RFC 2046 media type format: https://tools.ietf.org/html/rfc2046
    /// </summary>
    [JsonPropertyName("datacontenttype")]
    string? DataContentType { get; }

    #endregion // DataContentType

    #region DataSchemaPrefix

    /// <summary>
    /// A prefix of the schema.
    /// Examples:
    /// - "urn://json-schemamanager/"
    /// </summary>
    [JsonPropertyName("dataschema")]
    string? DataSchemaPrefix { get; }

    #endregion // DataSchemaPrefix

    /// <summary>
    /// Gets a value indicating whether this schema provider supports validation of data against schemas.
    /// When true, the ValidateAsync method can be used to validate data.
    /// When false, validation is not supported and ValidateAsync may throw NotSupportedException.
    /// </summary>
    bool SupportsValidation { get; }

    /// <summary>
    /// Validates the provided data against the schema.
    /// </summary>
    /// <typeparam name="TData">The type of data to validate</typeparam>
    /// <param name="data">The data to validate</param>
    /// <param name="type">The data type use as schema suffix.</param>
    /// <param name="dataSchema">Identifies the schema that data adheres to. Incompatible changes to the schema SHOULD be reflected by a different URI.</param>
    /// <returns>True if validation passes, false otherwise</returns>
    Task<bool> ValidateAsync<TData>(TData data, string type, string? dataSchema = null);

    /// <summary>
    /// Serializes the provided data into a byte array format according to the schema provider's implementation.
    /// </summary>
    /// <typeparam name="TData">The type of data to serialize</typeparam>
    /// <param name="data">The data to serialize</param>
    /// <returns>Serialized data as byte array</returns>
    byte[] Serialize<TData>(TData data);
}

