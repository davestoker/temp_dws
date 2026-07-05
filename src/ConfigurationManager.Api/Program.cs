using ConfigurationManager.Api.Auth;
using ConfigurationManager.Core.Interfaces;
using ConfigurationManager.Core.Services;
using ConfigurationManager.Data;
using ConfigurationManager.Data.Repositories;
using ConfigurationManager.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.Configure<SecretProtectorOptions>(configuration.GetSection("Secrets"));
        services.Configure<List<ApiKeyEntry>>(list => configuration.GetSection("ApiKeys").Bind(list));

        // Database provider is switchable via configuration so the same code path targets SQLite
        // for local dev and Azure SQL in production - see docs/database-design.md.
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("ConfigurationDb")
            ?? "Data Source=configurationmanager.db";

        services.AddDbContext<ConfigurationDbContext>(options =>
        {
            if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<IConfigurationRepository>(sp => sp.GetRequiredService<EfConfigurationRepository>());
        services.AddScoped<IConfigurationAdminRepository>(sp => sp.GetRequiredService<EfConfigurationRepository>());
        services.AddScoped<EfConfigurationRepository>();

        services.AddScoped<ISecretProtector, AesSecretProtector>();
        services.AddScoped<IConfigurationResolver, ConfigurationResolver>();
        services.AddScoped<AuditLogger>();
        services.AddScoped<ConfigurationKeyService>();
        services.AddScoped<ConfigurationValueService>();
        services.AddScoped<ReferenceDataService>();

        services.AddSingleton<ApiKeyAuthenticator>();

        services.AddApplicationInsightsTelemetryWorkerService();
    })
    .Build();

// Local-dev / demo convenience: apply migrations and seed example data on startup.
// TODO(production): remove automatic migration-on-startup; run migrations via a controlled
// deployment/release pipeline step instead.
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
    var secretProtector = scope.ServiceProvider.GetRequiredService<ISecretProtector>();
    await DbInitializer.SeedAsync(db, secretProtector);
}

host.Run();
