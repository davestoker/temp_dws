using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ConfigurationManager.Data;

/// <summary>
/// Design-time factory so `dotnet ef migrations add` / `database update` can run directly
/// against this project without needing the Api or Web host. Uses SQLite, matching local dev.
/// The Api/Web projects configure the real DbContextOptions (including provider selection)
/// at runtime via dependency injection - this factory is only used by EF Core tooling.
/// </summary>
public class ConfigurationDbContextFactory : IDesignTimeDbContextFactory<ConfigurationDbContext>
{
    public ConfigurationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ConfigurationDbContext>();
        optionsBuilder.UseSqlite("Data Source=configurationmanager.db");
        return new ConfigurationDbContext(optionsBuilder.Options);
    }
}
