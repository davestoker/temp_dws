using ConfigurationManager.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfigurationManager.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TenantCode).IsRequired().HasMaxLength(100);
        builder.HasIndex(t => t.TenantCode).IsUnique();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.CreatedBy).IsRequired().HasMaxLength(200);
        builder.Property(t => t.UpdatedBy).HasMaxLength(200);
    }
}
