namespace OutboxPlayground.Infra.Abstractions;

/// <summary>
/// Builder interface for configuring CloudEvent source, schema, and type information.
/// </summary>
public interface ICloudEventBuilderSource
{   
    /// <summary>
    /// Adds a data schema and its corresponding provider to the CloudEvent builder.
    /// </summary>
    /// <param name="dataSchemaProvider">The provider for serializing data according to the schema</param>
    /// <returns>A new builder instance with schema configuration</returns>
    ICloudEventBuilderSource AddSchema(IDataSchemaProvider dataSchemaProvider);

    /// <summary>
    /// Adds the event type to the CloudEvent builder.
    /// </summary>
    /// <param name="type">A value describing the type of event related to the originating occurrence</param>
    /// <returns>A CloudEvent builder ready to create events</returns>
    ICloudEventBuilder AddType(string type);
}

