using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public static async Task ApplyMigrationsAsync(this IServiceScopeFactory scopeFactory)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
