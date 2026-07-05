using ConfigurationManager.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfigurationManager.Data.Configurations;

public class IntegrationTypeConfiguration : IEntityTypeConfiguration<IntegrationType>
{
    public void Configure(EntityTypeBuilder<IntegrationType> builder)
    {
        builder.ToTable("IntegrationTypes");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Name).IsRequired().HasMaxLength(150);
        builder.HasIndex(i => i.Name).IsUnique();
        builder.Property(i => i.Description).HasMaxLength(1000);
        builder.Property(i => i.CreatedBy).IsRequired().HasMaxLength(200);
        builder.Property(i => i.UpdatedBy).HasMaxLength(200);
    }
}
