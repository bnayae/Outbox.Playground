namespace OutboxPlayground.Infra.Abstractions;

/// <summary>
/// Builder interface for creating CloudEvent instances with data payload or data reference.
/// </summary>
public interface ICloudEventBuilder
{
    /// <summary>
    /// Builds a CloudEvent with the provided data payload and auto-generated ID.
    /// </summary>
    /// <typeparam name="TData">The type of the data payload</typeparam>
    /// <param name="data">The data payload to include in the event</param>
    /// <returns>A new CloudEvent instance</returns>
    Task<CloudEvent> BuildAsync<TData>(TData data);

    /// <summary>
    /// Builds a CloudEvent with the provided ID and data payload.
    /// </summary>
    /// <typeparam name="TId">The type of the event identifier</typeparam>
    /// <typeparam name="TData">The type of the data payload</typeparam>
    /// <param name="id">The unique identifier for the event</param>
    /// <param name="data">The data payload to include in the event</param>
    /// <returns>A new CloudEvent instance</returns>
    Task<CloudEvent> BuildAsync<TId, TData>(TId id, TData data);

    /// <summary>
    /// Builds a CloudEvent with a data reference (claim check pattern) and auto-generated ID.
    /// </summary>
    /// <param name="data">The reference URL to the external data location</param>
    /// <returns>A new CloudEvent instance with data reference</returns>
    CloudEvent DataRefBuild(string data);

    /// <summary>
    /// Builds a CloudEvent with the provided ID and data reference (claim check pattern).
    /// </summary>
    /// <typeparam name="TId">The type of the event identifier</typeparam>
    /// <param name="id">The unique identifier for the event</param>
    /// <param name="data">The reference URL to the external data location</param>
    /// <returns>A new CloudEvent instance with data reference</returns>
    CloudEvent DataRefBuild<TId>(TId id, string data);
}

