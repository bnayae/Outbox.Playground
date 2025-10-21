using Microsoft.Extensions.Logging;

namespace OutboxPlayground.Infra.DataSchemaProviders.OutboxAvroSchemaProvider;

/// <summary>
/// High-performance logging definitions for AvroDataSchemaProvider using LoggerMessage source generator.
/// </summary>
internal static partial class Logs
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Schema validation failed for type '{Type}': {ErrorMessage}")]
    internal static partial void SchemaValidationFailed(this ILogger logger, string type, string errorMessage, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Schema not found for subject '{Subject}'")]
    internal static partial void SchemaNotFound(this ILogger logger, string subject);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to serialize data of type '{DataType}': {ErrorMessage}")]
    internal static partial void SerializationFailed(this ILogger logger, string dataType, string errorMessage, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to retrieve schema for subject '{Subject}': {ErrorMessage}")]
    internal static partial void SchemaRetrievalFailed(this ILogger logger, string subject, string errorMessage, Exception exception);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Successfully retrieved schema for subject '{Subject}' with ID {SchemaId}")]
    internal static partial void SchemaRetrieved(this ILogger logger, string subject, int schemaId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Serialized data for type '{DataType}' to {ByteCount} bytes")]
    internal static partial void DataSerialized(this ILogger logger, string dataType, int byteCount);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Schema validation succeeded for data type '{DataType}' against schema '{SchemaName}'")]
    internal static partial void SchemaValidationSucceeded(this ILogger logger, string dataType, string schemaName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Schema validation failed for data type '{DataType}' against schema '{SchemaName}': {ErrorMessage}")]
    internal static partial void SchemaValidationDetailsFailed(this ILogger logger, string dataType, string schemaName, string errorMessage);
}