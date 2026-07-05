using ConfigurationManager.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfigurationManager.Data.Configurations;

public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("AuditLogEntries");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.EntityName).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(50);
        builder.Property(a => a.OldValue).HasMaxLength(4000);
        builder.Property(a => a.NewValue).HasMaxLength(4000);
        builder.Property(a => a.Details).HasMaxLength(1000);
        builder.Property(a => a.ChangedBy).IsRequired().HasMaxLength(200);
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
    }
}
