using OutboxPlayground.Infra.Abstractions;
using OutboxPlayground.Infra.DataSchemaProviders.OutboxJsonSchemaProvider;
using System.Text.Json;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods for registering JSON data schema provider services.
/// </summary>
public static class JsonDataSchemaProviderRegistration
{
    /// <summary>
    /// Adds JSON data schema provider services to the dependency injection container.
    /// Registers the provider both as a singleton and as a keyed service using its data content type.
    /// </summary>
    /// <param name="services">The service collection to add the provider to.</param>
    /// <param name="options">Optional JSON serializer options to customize JSON serialization behavior. If null, default options will be used.</param>
    /// <returns>The same service collection for method chaining.</returns>
    /// <remarks>
    /// This method registers the JSON data schema provider in two ways:
    /// <list type="bullet">
    /// <item><description>As a singleton <see cref="IDataSchemaProvider"/> for general dependency injection</description></item>
    /// <item><description>As a keyed singleton using the provider's DataContentType for content-type-specific resolution</description></item>
    /// </list>
    /// The JSON provider supports serialization of data into JSON format using System.Text.Json.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register with default options
    /// services.AddJsonDataSchemaProvider();
    /// 
    /// // Register with custom JSON options
    /// var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    /// services.AddJsonDataSchemaProvider(jsonOptions);
    /// </code>
    /// </example>
    public static IServiceCollection AddJsonDataSchemaProvider(
                                            this IServiceCollection services,
                                            JsonSerializerOptions? options = null)
    {
        IDataSchemaProvider instance = new JsonDataSchemaProvider(options);
        services.AddSingleton(instance);
        services.AddKeyedSingleton(instance.DataContentType, instance);
        return services;
    }
}
