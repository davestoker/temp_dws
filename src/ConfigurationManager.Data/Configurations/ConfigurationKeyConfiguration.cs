using ConfigurationManager.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfigurationManager.Data.Configurations;

public class ConfigurationKeyConfiguration : IEntityTypeConfiguration<ConfigurationKey>
{
    public void Configure(EntityTypeBuilder<ConfigurationKey> builder)
    {
        builder.ToTable("ConfigurationKeys");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.KeyName).IsRequired().HasMaxLength(200);
        builder.HasIndex(k => k.KeyName).IsUnique();
        builder.Property(k => k.DisplayName).IsRequired().HasMaxLength(200);
        builder.Property(k => k.Description).HasMaxLength(2000);
        builder.Property(k => k.DefaultValue).HasMaxLength(4000);
        builder.Property(k => k.ValidationRules).HasMaxLength(1000);
        builder.Property(k => k.ExampleValue).HasMaxLength(1000);
        builder.Property(k => k.HelpText).HasMaxLength(2000);
        builder.Property(k => k.DataType).HasConversion<string>().HasMaxLength(20);
        builder.Property(k => k.VisibilityGroup).HasConversion<string>().HasMaxLength(20);
        builder.Property(k => k.CreatedBy).IsRequired().HasMaxLength(200);
        builder.Property(k => k.UpdatedBy).HasMaxLength(200);

        builder.HasOne(k => k.IntegrationType)
            .WithMany()
            .HasForeignKey(k => k.IntegrationTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(k => k.Section)
            .WithMany()
            .HasForeignKey(k => k.SectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(k => k.Subsection)
            .WithMany()
            .HasForeignKey(k => k.SubsectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
