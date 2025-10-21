using Confluent.SchemaRegistry;
using Microsoft.Extensions.DependencyInjection;
using OutboxPlayground.Infra.Abstractions;

namespace OutboxPlayground.Infra.DataSchemaProviders.OutboxAvroSchemaProvider;

public static class ServiceCollectionExtensions
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