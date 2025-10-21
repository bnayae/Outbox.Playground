using Confluent.SchemaRegistry;
using OutboxPlayground.Infra.Abstractions;

using OutboxPlayground.Infra.DataSchemaProviders.OutboxAvroSchemaProvider;

namespace Microsoft.Extensions.DependencyInjection;

public static class AvroDataSchemaProviderRegistration
{
    public static IServiceCollection AddAvroDataSchemaProvider(this IServiceCollection services,
                                                               string? schemaRegistryUrl = null)
    {
        services.AddSingleton<ISchemaRegistryClient>(_ =>
            new CachedSchemaRegistryClient(new SchemaRegistryConfig
            {
                Url = schemaRegistryUrl ?? "http://localhost:8081"
            }));

        services.AddSingleton<IDataSchemaProvider, AvroDataSchemaProvider>();
        services.AddKeyedSingleton<IDataSchemaProvider>("application/avro", (sp, _) => sp.GetRequiredService<IDataSchemaProvider>());

        return services;
    }
}