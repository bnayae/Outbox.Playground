using Microsoft.EntityFrameworkCore;
using OutboxPlayground.Infra.Abstractions;
using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.EFRepository;
/// <summary>
/// EF Core DbContext for managing payments and outbox events.
/// </summary>
internal class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {

    }

    public DbSet<PaymentEntity> Payments { get; set; }

    public DbSet<User> Users { get; set; }

    // Outbox table for storing events to be published
    public DbSet<CloudEvent> Outbox { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure PaymentMessage entity
        modelBuilder.Entity<PaymentEntity>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Status)
                  .HasConversion<string>();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(p => p.Id);
        });

        // Configure Outbox entity 
        modelBuilder.CreatingOutboxModel();
    }
}
