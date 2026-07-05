using ConfigurationManager.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfigurationManager.Data.Configurations;

public class IntegrationConfiguration : IEntityTypeConfiguration<Integration>
{
    public void Configure(EntityTypeBuilder<Integration> builder)
    {
        builder.ToTable("Integrations");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.IntegrationName).IsRequired().HasMaxLength(150);
        builder.HasIndex(i => i.IntegrationName).IsUnique();
        builder.Property(i => i.Description).HasMaxLength(1000);
        builder.Property(i => i.CreatedBy).IsRequired().HasMaxLength(200);
        builder.Property(i => i.UpdatedBy).HasMaxLength(200);

        builder.HasOne(i => i.IntegrationType)
            .WithMany()
            .HasForeignKey(i => i.IntegrationTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
