using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace OutboxPlayground.Infra.Abstractions;

/// <summary>
/// Represents a CloudEvent as defined by the CloudEvents specification v1.0.
/// CloudEvents is a specification for describing event data in common formats to provide interoperability across services, platforms and systems.
/// </summary>
public record CloudEvent
{
    /// <summary>
    /// The CloudEvents specification version constant.
    /// </summary>
    public const string SPEC_VERSION = "1.0";

    #region SpecVersion

    /// <summary>
    /// The version of the CloudEvents specification which the event uses.
    /// This enables the interpretation of the context. Compliant event producers MUST use a value of "1.0" when referring to this version of the specification.
    /// </summary>
    [JsonPropertyName("specversion")]
    public string SpecVersion { get; init; } = SPEC_VERSION;

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

    #region TraceParent

    /// <summary>
    /// Contains a version, trace ID, span ID, and trace flags as defined in the W3C Trace Context specification.
    /// This extension enables distributed tracing scenarios by allowing events to carry trace context information.
    /// The value is formatted according to the traceparent header format: version-traceid-spanid-traceflags
    /// Example: "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
    /// </summary>
    [JsonPropertyName("traceparent")]
    public OtelTraceParent? TraceParent { get; init; }

    #endregion // TraceParent


    /// <summary>
    /// Creates a new CloudEventBuilder instance for building CloudEvents with the specified source.
    /// </summary>
    /// <param name="source">The context in which events will happen</param>
    /// <param name="timeProvider">Optional time provider for generating timestamps. Uses System time provider if not specified.</param>
    /// <returns>A new CloudEventBuilder instance</returns>
    public static ICloudEventBuilderSource CreateBuilder(string source, TimeProvider? timeProvider = null)
    {
        var result = new CloudEventBuilder(source, timeProvider ?? TimeProvider.System);
        return result;
    }
}

