using System.Diagnostics;
using System.Text.Json.Serialization;

namespace OutboxPlayground.Infra.Abstractions;

/// <summary>
/// Immutable builder for creating CloudEvent instances following the builder pattern.
/// Implements both source configuration and event building capabilities.
/// </summary>
internal readonly record struct CloudEventBuilder :
                                    ICloudEventBuilderSource,
                                    ICloudEventBuilder
{
    private readonly TimeProvider _timeProvider;

    #region Ctor

    /// <summary>
    /// Initializes a new CloudEventBuilder with the specified source and time provider.
    /// </summary>
    /// <param name="source">The context in which the event happened</param>
    /// <param name="timeProvider">Provider for generating timestamps</param>
    public CloudEventBuilder(string source, TimeProvider timeProvider)
    {
        Source = source;
        _timeProvider = timeProvider;
    }

    #endregion //  Ctor

    #region Type

    /// <summary>
    /// This attribute contains a value describing the type of event related to the originating occurrence.
    /// Often this attribute is used for routing, observability, policy enforcement, etc.   
    /// Examples:
    /// - "user-created/v1.0"
    /// </summary>
    public string Type { get; init; } = string.Empty;

    #endregion // Type

    #region PartitionKey

    /// <summary>
    /// Partition Key groups related events together for processing (e.g., by user ID). 
    /// Used for sharding and maintaining event order per entity. 
    /// The value should be parse-able by the consumer, and relate to the datacontenttype. 
    /// a JSON should use JSONPath, Avro should use dot notation.
    /// </summary>
    public string PartitionKey { get; init; } = Guid.NewGuid().ToString();

    #endregion // PartitionKey

    #region Source

    /// <summary>
    /// Identifies the context in which an event happened. Often this will include information such as the type of the event source, 
    /// the organization publishing the event or the process that produced the event.
    /// Examples:
    /// - "clm"
    /// </summary>
    [JsonPropertyName("source")]
    public string Source { get; init; }

    #endregion // Source

    #region DataSchemaProvider

    /// <summary>
    /// The dataRef schema provider responsible for serializing event dataRef according to the specified schema.
    /// </summary>
    public IDataSchemaProvider? DataSchemaProvider { get; init; }

    #endregion //  DataSchemaProvider

    #region AddSchema

    /// <summary>
    /// Adds a dataRef schema and its corresponding provider to create a new builder instance.
    /// </summary>
    /// <param name="dataSchemaProvider">The schema provider for serialization</param>
    /// <returns>A new builder instance with schema configuration</returns>
    ICloudEventBuilderSource ICloudEventBuilderSource.AddSchema(IDataSchemaProvider dataSchemaProvider)
    {
        return this with { DataSchemaProvider = dataSchemaProvider };
    }

    #endregion //  AddSchema

    #region AddType

    /// <summary>
    /// Adds the event type to create a new builder instance ready for building events.
    /// </summary>
    /// <param name="type">The event type</param>
    /// <returns>A builder instance configured for creating events</returns>
    ICloudEventBuilder ICloudEventBuilderSource.AddType(string type) => this with { Type = type };

    #endregion //  AddType

    #region AddPartition

    /// <summary>
    /// Partition Key groups related events together for processing (e.g., by user ID). 
    /// Used for sharding and maintaining event order per entity. 
    /// The value should be parse-able by the consumer, and relate to the datacontenttype. 
    /// a JSON should use JSONPath, Avro should use dot notation.
    /// </summary>
    /// <typeparam name="TPartition"></typeparam>
    /// <param name="partitionKey"></param>
    /// <returns></returns>
    ICloudEventBuilder ICloudEventBuilder.AddPartition<TPartition>(TPartition partitionKey)
    {
        return this with
        {
            PartitionKey = partitionKey?.ToString()
                                                   ?? Guid.NewGuid().ToString()
        };
    }

    #endregion //  AddPartition

    #region Build

    /// <summary>
    /// Builds a CloudEvent with auto-generated ID and the provided dataRef payload.
    /// </summary>
    /// <typeparam name="TData">The type of the dataRef payload</typeparam>
    /// <param name="data">The dataRef to include in the event</param>
    /// <param name="sequence">
    /// Ordering indicator for events. Monotonically increasing value to determine event sequence within a chain.
    /// </param>
    /// <returns>A new CloudEvent instance</returns>
    async Task<CloudEvent> ICloudEventBuilder.BuildAsync<TData>(TData data,
                                                                long? sequence)
    {
        ICloudEventBuilder self = this;
        var id = Guid.NewGuid();
        return await self.BuildAsync(id, data, sequence);
    }

    /// <summary>
    /// Builds a CloudEvent with the specified ID and dataRef payload.
    /// </summary>
    /// <typeparam name="TId">The type of the event identifier</typeparam>
    /// <typeparam name="TData">The type of the dataRef payload</typeparam>
    /// <param name="id">The unique identifier for the event</param>
    /// <param name="data">The dataRef payload</param>
    /// <param name="sequence">
    /// Ordering indicator for events. Monotonically increasing value to determine event sequence within a chain.
    /// </param>
    /// <returns>A new CloudEvent instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when id is null</exception>
    async Task<CloudEvent> ICloudEventBuilder.BuildAsync<TId, TData>(TId id,
                                                                     TData data,
                                                                     long? sequence)
    {
        #region Validation

        if (DataSchemaProvider is null)
        {
            throw new InvalidOperationException("DataSchemaProvider is not configured.");
        }

        #endregion //  Validation

        string? dataSchema = DataSchemaProvider.DataSchemaPrefix is not null
                                ? $"{DataSchemaProvider.DataSchemaPrefix}{Type}"
                                : null;

        if (!await DataSchemaProvider.ValidateAsync(data, Type))
        {
            throw new InvalidOperationException("Data validation against schema failed.");
        }

        OtelTraceParent? traceParent = Activity.Current?.SerializeTelemetryContext();
        byte[] buffer = await DataSchemaProvider.SerializeAsync(data);
        return new CloudEvent()
        {
            Type = Type,
            Source = Source,
            Id = id?.ToString() ?? throw new ArgumentNullException(nameof(id)),
            Time = _timeProvider.GetUtcNow(),
            DataContentType = DataSchemaProvider.DataContentType,
            DataSchema = dataSchema,
            TraceParent = traceParent,
            PartitionKey = PartitionKey,
            Sequence = sequence,
            Data = buffer
        };
    }

    #endregion //  BuildAsync

    #region DataRefBuild

    /// <summary>
    /// Builds a CloudEvent with auto-generated ID and a dataRef reference.
    /// </summary>
    /// <param name="dataRef">The reference URL to external dataRef location</param>
    /// <param name="sequence">
    /// Ordering indicator for events. Monotonically increasing value to determine event sequence within a chain.
    /// </param>
    /// <returns>A new CloudEvent instance with dataRef reference</returns>
    CloudEvent ICloudEventBuilder.DataRefBuild(string dataRef, long? sequence)
    {
        ICloudEventBuilder self = this;
        var id = Guid.NewGuid();
        return self.DataRefBuild(id, dataRef, sequence);
    }

    /// <summary>
    /// Builds a CloudEvent with the specified ID and dataRef reference.
    /// </summary>
    /// <typeparam name="TId">The type of the event identifier</typeparam>
    /// <param name="id">The unique identifier for the event</param>
    /// <param name="dataRef">The reference URL to external data location</param>
    /// <returns>A new CloudEvent instance with dataRef reference</returns>
    /// <param name="sequence">
    /// Ordering indicator for events. Monotonically increasing value to determine event sequence within a chain.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when id is null</exception>
    CloudEvent ICloudEventBuilder.DataRefBuild<TId>(TId id, string dataRef, long? sequence)
    {
        OtelTraceParent? traceParent = Activity.Current?.SerializeTelemetryContext();
        return new CloudEvent()
        {
            Type = Type,
            Source = Source,
            Id = id?.ToString() ?? throw new ArgumentNullException(nameof(id)),
            Time = _timeProvider.GetUtcNow(),
            TraceParent = traceParent,
            PartitionKey = PartitionKey,
            Sequence = sequence,
            DataRef = dataRef
        };
    }

    #endregion //  DataRefBuild
}

