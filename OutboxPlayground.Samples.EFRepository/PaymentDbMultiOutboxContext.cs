using Microsoft.EntityFrameworkCore;
using OutboxPlayground.Infra.Abstractions;
using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.EFRepository;
/// <summary>
/// EF Core DbContext for managing payments and outbox events.
/// </summary>
internal class PaymentDbMultiOutboxContext : DbContext
{
    public PaymentDbMultiOutboxContext(DbContextOptions<PaymentDbMultiOutboxContext> options) : base(options)
    {

    }

    public DbSet<PaymentEntity> Payments { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<CloudEvent> Outbox => Set<CloudEvent>("Outbox");

    public DbSet<CloudEvent> HighRiskOutbox => Set<CloudEvent>("HighRiskOutbox"); // sample of multiple outbox within a single context


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

        modelBuilder.CreatingOutboxModel("Outbox", "HighRiskOutbox");
    }
}
