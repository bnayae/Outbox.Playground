using Microsoft.EntityFrameworkCore;
using OutboxPlayground.Samples.Abstractions;
using OutboxPlayground.Samples.EFRepository;

namespace Microsoft.Extensions.DependencyInjection;

public static class EFRepositoryDIExtensions
{
    public static IServiceCollection AddPaymentRepository(this IServiceCollection services, string connStr = "PaymentConnection")
    {
        services.AddDbContextFactory<PaymentDbContext>(optionsBuilder =>
            {
                optionsBuilder.UseSqlServer(connStr, sqlServerOptions =>
                {
                    sqlServerOptions.CommandTimeout(30);
                    sqlServerOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(0.2), null);
                });
            });
        services.AddDbContextFactory<PaymentDbMultiOutboxContext>(optionsBuilder =>
            {
                optionsBuilder.UseSqlServer(connStr, sqlServerOptions =>
                {
                    sqlServerOptions.CommandTimeout(30);
                    sqlServerOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(0.2), null);
                });
            });

        services.AddKeyedScoped<IPaymentRepository, PaymentRepository>("default");
        services.AddKeyedScoped<IPaymentRepository, PaymentMultiOutboxRepository>("multi-outbox");
        return services;
    }
}
