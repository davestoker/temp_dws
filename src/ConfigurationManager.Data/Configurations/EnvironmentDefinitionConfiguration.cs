using ConfigurationManager.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfigurationManager.Data.Configurations;

public class EnvironmentDefinitionConfiguration : IEntityTypeConfiguration<EnvironmentDefinition>
{
    public void Configure(EntityTypeBuilder<EnvironmentDefinition> builder)
    {
        builder.ToTable("Environments");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(e => e.Name).IsUnique();
        builder.Property(e => e.CreatedBy).IsRequired().HasMaxLength(200);
        builder.Property(e => e.UpdatedBy).HasMaxLength(200);
    }
}
