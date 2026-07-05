using ConfigurationManager.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConfigurationManager.Data;

public class ConfigurationDbContext : DbContext
{
    public ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<EnvironmentDefinition> Environments => Set<EnvironmentDefinition>();
    public DbSet<IntegrationType> IntegrationTypes => Set<IntegrationType>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<ConfigurationSection> ConfigurationSections => Set<ConfigurationSection>();
    public DbSet<ConfigurationSubsection> ConfigurationSubsections => Set<ConfigurationSubsection>();
    public DbSet<ConfigurationKey> ConfigurationKeys => Set<ConfigurationKey>();
    public DbSet<ConfigurationValue> ConfigurationValues => Set<ConfigurationValue>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConfigurationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
