using Microsoft.EntityFrameworkCore;
using OutboxPlayground.Samples.Abstractions;
using OutboxPlayground.Samples.EFRepository;

namespace Microsoft.Extensions.DependencyInjection;

public static class EFRepositoryDIExtenssions
{
    public static IServiceCollection AddPaymentRepository(this IServiceCollection services, string connStr = "PaymentConnection")
    {
        services.AddDbContext<PaymentDbContext>(options =>
        {
            options.UseSqlServer(connStr);
        });
        services.AddScoped<IPaymentRepository, Paymentrepository>();
        return services;
    }

    public static async Task BootstrapDatabaseAsync(this IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

        // Ensure database is created first
        //await dbContext.Database.EnsureCreatedAsync();

        // Then apply any pending migrations
        //await dbContext.Database.MigrateAsync();
    }
}
