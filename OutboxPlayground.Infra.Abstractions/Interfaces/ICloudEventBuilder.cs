namespace OutboxPlayground.Infra.Abstractions;

/// <summary>
/// Builder interface for creating CloudEvent instances with dataRef payload or dataRef reference.
/// </summary>
public interface ICloudEventBuilder
{
    /// <summary>
    /// Builds a CloudEvent with the provided dataRef payload and auto-generated ID.
    /// </summary>
    /// <typeparam name="TData">The type of the dataRef payload</typeparam>
    /// <param name="data">The dataRef payload to include in the event</param>
    /// <returns>A new CloudEvent instance</returns>
    Task<CloudEvent> BuildAsync<TData>(TData data);

    /// <summary>
    /// Builds a CloudEvent with the provided ID and dataRef payload.
    /// </summary>
    /// <typeparam name="TId">The type of the event identifier</typeparam>
    /// <typeparam name="TData">The type of the dataRef payload</typeparam>
    /// <param name="id">The unique identifier for the event</param>
    /// <param name="data">The dataRef payload to include in the event</param>
    /// <returns>A new CloudEvent instance</returns>
    Task<CloudEvent> BuildAsync<TId, TData>(TId id, TData data);

    /// <summary>
    /// Builds a CloudEvent with a dataRef reference (claim check pattern) and auto-generated ID.
    /// </summary>
    /// <param name="dataRef">The reference URL to the external dataRef location</param>
    /// <returns>A new CloudEvent instance with dataRef reference</returns>
    CloudEvent DataRefBuild(string dataRef);

    /// <summary>
    /// Builds a CloudEvent with the provided ID and dataRef reference (claim check pattern).
    /// </summary>
    /// <typeparam name="TId">The type of the event identifier</typeparam>
    /// <param name="id">The unique identifier for the event</param>
    /// <param name="dataRef">The reference URL to the external dataRef location</param>
    /// <returns>A new CloudEvent instance with dataRef reference</returns>
    CloudEvent DataRefBuild<TId>(TId id, string dataRef);
}

