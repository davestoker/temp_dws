using ConfigurationManager.Data;
using Microsoft.EntityFrameworkCore;

namespace ConfigurationManager.Tests;

internal static class TestDbContextFactory
{
    public static ConfigurationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ConfigurationDbContext(options);
    }
}
