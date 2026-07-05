using ConfigurationManager.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfigurationManager.Data.Configurations;

public class ConfigurationSectionConfiguration : IEntityTypeConfiguration<ConfigurationSection>
{
    public void Configure(EntityTypeBuilder<ConfigurationSection> builder)
    {
        builder.ToTable("ConfigurationSections");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
        builder.HasIndex(s => s.Name).IsUnique();
        builder.Property(s => s.CreatedBy).IsRequired().HasMaxLength(200);
        builder.Property(s => s.UpdatedBy).HasMaxLength(200);

        builder.HasMany(s => s.Subsections)
            .WithOne(sub => sub.Section)
            .HasForeignKey(sub => sub.SectionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ConfigurationSubsectionConfiguration : IEntityTypeConfiguration<ConfigurationSubsection>
{
    public void Configure(EntityTypeBuilder<ConfigurationSubsection> builder)
    {
        builder.ToTable("ConfigurationSubsections");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(150);
        builder.HasIndex(s => new { s.SectionId, s.Name }).IsUnique();
        builder.Property(s => s.CreatedBy).IsRequired().HasMaxLength(200);
        builder.Property(s => s.UpdatedBy).HasMaxLength(200);
    }
}
