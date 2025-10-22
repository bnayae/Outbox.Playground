using OutboxPlayground.Infra.Abstractions;

using OutboxPlayground.Infra.DataSchemaProviders.OutboxAvroSchemaProvider;

namespace Microsoft.Extensions.DependencyInjection;

public static class AvroDataSchemaProviderRegistration
{
    public static IServiceCollection AddAvroDataSchemaProvider(this IServiceCollection services)
    {
        services.AddSingleton<IDataSchemaProvider, AvroDataSchemaProvider>();
        services.AddKeyedSingleton<IDataSchemaProvider>("application/avro", (sp, _) => sp.GetRequiredService<IDataSchemaProvider>());

        return services;
    }
}