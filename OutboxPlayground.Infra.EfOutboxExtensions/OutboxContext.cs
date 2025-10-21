using Microsoft.EntityFrameworkCore;
using OutboxPlayground.Infra.Abstractions;

namespace OutboxPlayground.Infra.EfOutboxExtensions;


public abstract class OutboxContext : DbContext
{
    public OutboxContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<CloudEvent> Outbox { get; set; }


    protected void ModelCreatingOutbox(ModelBuilder modelBuilder)
    {
        // Configure CloudEvent entity for outbox pattern

        modelBuilder.Entity<CloudEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.SpecVersion)
                  .IsRequired()
                  .HasMaxLength(10);

            entity.Property(e => e.Type)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(e => e.Source)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(e => e.Id)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(e => e.Time)
                  .IsRequired();

            entity.Property(e => e.DataContentType)
                  .HasMaxLength(255);

            entity.Property(e => e.DataSchema)
                  .HasMaxLength(500);

            entity.Property(e => e.Subject)
                  .HasMaxLength(255);

            entity.Property(e => e.DataRef)
                  .HasMaxLength(500);

            entity.Property(e => e.TraceParent)
                  .HasMaxLength(55) // W3C Trace Context traceparent format: "00-{32 hex}-{16 hex}-{2 hex}" = 55 chars
                  .HasConversion<string?>(); // Convert OtelTraceParent to/from string for database storage

            // Index for efficient querying of outbox events
            entity.HasIndex(e => e.Time);
            entity.HasIndex(e => new { e.Source, e.Type });
        });

        base.OnModelCreating(modelBuilder);
    }
}
