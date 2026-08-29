using Catalog.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace Catalog.IntegrationTests;

public class CatalogApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CatalogDb"] = _postgres.GetConnectionString()
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var db = new CatalogDbContext(options);
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}