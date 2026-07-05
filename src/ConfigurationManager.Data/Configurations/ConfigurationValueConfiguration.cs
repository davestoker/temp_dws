using ConfigurationManager.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfigurationManager.Data.Configurations;

public class ConfigurationValueConfiguration : IEntityTypeConfiguration<ConfigurationValue>
{
    public void Configure(EntityTypeBuilder<ConfigurationValue> builder)
    {
        builder.ToTable("ConfigurationValues");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Value).HasMaxLength(4000);
        builder.Property(v => v.EncryptedValue).HasMaxLength(4000);
        builder.Property(v => v.CreatedBy).IsRequired().HasMaxLength(200);
        builder.Property(v => v.UpdatedBy).HasMaxLength(200);

        // Intended to enforce one row per (key, tenant, environment) combination. NOTE: both SQL
        // Server and SQLite treat NULL as distinct in unique indexes, so this does not by itself
        // prevent duplicate Base (Tenant=null, Environment=null) rows - the application layer
        // (ConfigurationValueService.UpsertAsync, via FindValueAsync) is the real guard against
        // duplicates. Kept here as a defence-in-depth constraint for the non-null combinations.
        builder.HasIndex(v => new { v.ConfigurationKeyId, v.TenantId, v.EnvironmentId }).IsUnique();

        builder.HasOne(v => v.ConfigurationKey)
            .WithMany(k => k.Values)
            .HasForeignKey(v => v.ConfigurationKeyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Tenant)
            .WithMany()
            .HasForeignKey(v => v.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Environment)
            .WithMany()
            .HasForeignKey(v => v.EnvironmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
