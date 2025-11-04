namespace OutboxPlayground.Infra.Abstractions;

/// <summary>
/// Builder interface for creating CloudEvent instances with dataRef payload or dataRef reference.
/// </summary>
public interface ICloudEventBuilder
{
    /// <summary>
    /// Partition Key groups related events together for processing (e.g., by user ID). 
    /// Used for sharding and maintaining event order per entity. 
    /// The value should be parse-able by the consumer, and relate to the datacontenttype. 
    /// a JSON should use JSONPath, Avro should use dot notation.
    /// </summary>
    /// <typeparam name="TPartition"></typeparam>
    /// <param name="partitionKey"></param>
    /// <returns></returns>
    public ICloudEventBuilder AddPartition<TPartition>(TPartition partitionKey);

    /// <summary>
    /// Builds a CloudEvent with the provided dataRef payload and auto-generated ID.
    /// </summary>
    /// <typeparam name="TData">The type of the dataRef payload</typeparam>
    /// <param name="data">The dataRef payload to include in the event</param>
    /// <param name="sequence">
    /// Ordering indicator for events. Monotonically increasing value to determine event sequence within a chain.
    /// </param>
    /// <returns>A new CloudEvent instance</returns>
    Task<CloudEvent> BuildAsync<TData>(TData data, long? sequence = null);

    /// <summary>
    /// Builds a CloudEvent with the provided ID and dataRef payload.
    /// </summary>
    /// <typeparam name="TId">The type of the event identifier</typeparam>
    /// <typeparam name="TData">The type of the dataRef payload</typeparam>
    /// <param name="id">The unique identifier for the event</param>
    /// <param name="data">The dataRef payload to include in the event</param>
    /// <param name="sequence">
    /// Ordering indicator for events. Monotonically increasing value to determine event sequence within a chain.
    /// </param>
    /// <returns>A new CloudEvent instance</returns>
    Task<CloudEvent> BuildAsync<TId, TData>(TId id, TData data, long? sequence = null);


    /// <summary>
    /// Builds a CloudEvent with a dataRef reference (claim check pattern) and auto-generated ID.
    /// </summary>
    /// <param name="dataRef">The reference URL to the external dataRef location</param>
    /// <param name="sequence">
    /// Ordering indicator for events. Monotonically increasing value to determine event sequence within a chain.
    /// </param>
    /// <returns>A new CloudEvent instance with dataRef reference</returns>
    CloudEvent DataRefBuild(string dataRef, long? sequence = null);


    /// <summary>
    /// Builds a CloudEvent with the provided ID and dataRef reference (claim check pattern).
    /// </summary>
    /// <typeparam name="TId">The type of the event identifier</typeparam>
    /// <param name="id">The unique identifier for the event</param>
    /// <param name="dataRef">The reference URL to the external dataRef location</param>
    /// <param name="sequence">
    /// Ordering indicator for events. Monotonically increasing value to determine event sequence within a chain.
    /// </param>
    /// <returns>A new CloudEvent instance with dataRef reference</returns>
    CloudEvent DataRefBuild<TId>(TId id, string dataRef, long? sequence = null);
}

