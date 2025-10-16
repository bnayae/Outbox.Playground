using System.Text.Json.Serialization;

namespace OutboxPlayground.Infra.Abstractions;


/// <summary>
/// Represents a CloudEvent as defined by the CloudEvents specification v1.0.
/// </summary>
public class CloudEvent
{
    #region SpecVersion

    /// <summary>
    /// The version of the CloudEvents specification which the event uses.
    /// This enables the interpretation of the context. Compliant event producers MUST use a value of "1.0" when referring to this version of the specification.
    /// </summary>
    [JsonPropertyName("specversion")]
    public required string SpecVersion { get; init; }

    #endregion // SpecVersion

    #region Type

    /// <summary>
    /// This attribute contains a value describing the type of event related to the originating occurrence.
    /// Often this attribute is used for routing, observability, policy enforcement, etc.   
    /// Examples:
    /// - "user-created/v1.0"
    /// </summary>
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    #endregion // Type

    #region Source

    /// <summary>
    /// Identifies the context in which an event happened. Often this will include information such as the type of the event source, 
    /// the organization publishing the event or the process that produced the event.
    /// Examples:
    /// - "clm"
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    #endregion // Source

    #region Id

    /// <summary>
    /// Identifies the event. Producers MUST ensure that source + id is unique for each distinct event.
    /// If a duplicate event is re-sent (e.g. due to a network error) it MAY have the same id.
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    #endregion // Id

    #region Time

    /// <summary>
    /// Timestamp of when the occurrence happened. If the time of the occurrence cannot be determined then this attribute MAY be set to some other time 
    /// (such as the current time) by the CloudEvents producer, however all producers for the same source MUST be consistent in this respect.
    /// </summary>
    [JsonPropertyName("time")]
    public DateTimeOffset Time { get; init; } = DateTimeOffset.UtcNow;

    #endregion // Time

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
    public string? DataContentType { get; init; }

    #endregion // DataContentType

    #region DataSchema

    /// <summary>
    /// Identifies the schema that data adheres to. Incompatible changes to the schema SHOULD be reflected by a different URI.
    /// Examples:
    /// - "urn://schemamanager/clm/user-created/v1.0"
    /// </summary>
    [JsonPropertyName("dataschema")]
    public string? DataSchema { get; init; }

    #endregion // DataSchema

    #region Subject

    /// <summary>
    /// This describes the subject of the event in the context of the event producer (identified by source).
    /// In publish-subscribe scenarios, a subscriber will typically subscribe to events emitted by a source,
    /// but the source identifier alone might not be sufficient as a subscription filter.
    /// </summary>
    [JsonPropertyName("subject")]
    public string? Subject { get; init; }

    #endregion // Subject

    #region Data

    /// <summary>
    /// The event payload. This specification does not place any restriction on the type of this information.
    /// It is encoded into a media format which is specified by the datacontenttype attribute (e.g. application/json), and adheres to the dataschema format when those respective attributes are present.
    /// </summary>
    [JsonPropertyName("data")]
    public byte[]? Data { get; init; }

    #endregion // Data

    #region DataRef

    /// <summary>
    /// Reference to external location where event payload is stored (claim check pattern). 
    /// Authentication and Authorization should be considered when used. Interchangeable with data field.
    /// Example: http://s3.comp.com/path/to/specific/data.json
    /// </summary>
    [JsonPropertyName("dataref")]
    public string? DataRef { get; init; }

    #endregion // DataRef

    #region Create

    /// <summary>
    /// Creates a new CloudEvent with default values for common scenarios.
    /// </summary>
    /// <param name="type">The type of event</param>
    /// <param name="source">The source of the event</param>
    /// <param name="data">Optional event payload</param>
    /// <param name="subject">Optional subject of the event</param>
    /// <returns>A new CloudEvent instance</returns>
    public static CloudEvent Create(string type,
                                    string source,
                                    byte[]? data = null,
                                    string? subject = null)
    {
        var result = new CloudEvent()
        {
            SpecVersion = "1.0",
            Type = type,
            Source = source,
            Id = Guid.NewGuid().ToString(),
            Time = DateTime.UtcNow,
            DataContentType = "application/json",
            Subject = subject,
            Data = data
        };
        return result;
    }

    #endregion //  Create

    // TODO: CreateJson<T>
    // TODO: CreateProto<T>
    // TODO: CreateAvro<T>
}

