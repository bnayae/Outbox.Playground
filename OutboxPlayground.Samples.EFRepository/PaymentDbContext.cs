using Microsoft.EntityFrameworkCore;
using OutboxPlayground.Infra.Abstractions;
using OutboxPlayground.Infra.EfOutboxExtensions;
using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.EFRepository;

internal class PaymentDbContext : OutboxContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {

    }

    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Payment entity
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Status)
                  .HasConversion<string>();
        });

        modelBuilder.Entity<CloudEvent>().ToTable("MyOutbox");

        ModelCreatingOutbox(modelBuilder);
    }
}
