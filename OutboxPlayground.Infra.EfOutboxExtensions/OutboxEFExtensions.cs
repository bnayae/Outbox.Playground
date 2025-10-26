using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OutboxPlayground.Infra.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore;

[ExcludeFromCodeCoverage]
public static class OutboxEFExtensions
{
    /// <summary>
    /// Creates and configures the Entity Framework model for CloudEvent entities used in the outbox pattern.
    /// This method sets up the database schema including table structure, property configurations, 
    /// data type conversions, and indexes for efficient querying.
    /// </summary>
    /// <param name="modelBuilder">The ModelBuilder instance used to configure the entity model</param>
    /// <param name="tableNames"></param>
    /// <returns>The EntityTypeBuilder for CloudEvent to allow further configuration chaining</returns>
    /// <remarks>
    /// This configuration includes:
    /// - Primary key setup on the Id property
    /// - Required field validation and length constraints for CloudEvents specification compliance
    /// - Custom value conversion for OtelTraceParent to/from string for database storage
    /// - Performance indexes on Time and composite Source/Type fields for efficient outbox processing
    /// 
    /// The outbox pattern implementation stores CloudEvents temporarily before they are published,
    /// ensuring reliable event delivery in distributed systems.
    /// </remarks>
    public static void CreatingOutboxModel(this ModelBuilder modelBuilder, params string[] tableNames)
    {
        // Configure CloudEvent entity for outbox pattern

        bool hasNames = tableNames != null && tableNames.Length > 0;

        if (hasNames)
        {
            foreach (var tableName in tableNames ?? [])
            {
                var entity = modelBuilder.SharedTypeEntity<CloudEvent>(tableName);
                ConfigureEntity(entity);
            }
        }
        else
        { 
            var entity = modelBuilder.Entity<CloudEvent>();
            ConfigureEntity(entity);
        }

        void ConfigureEntity(EntityTypeBuilder<CloudEvent> entity)
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
                  .HasConversion<string?>(m => m.HasValue ? m.Value : string.Empty, m => string.IsNullOrEmpty(m) ? (OtelTraceParent?)null : OtelTraceParent.From(m)); // Convert OtelTraceParent to/from string for database storage

            // Index for efficient querying of outbox events
            entity.HasIndex(e => e.Time);
            entity.HasIndex(e => new { e.Source, e.Type });
        }
    }
}