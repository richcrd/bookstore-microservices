using System.Net.Http.Headers;
using System.Text;
using Catalog.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
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
                ["ConnectionStrings:CatalogDb"] = _postgres.GetConnectionString(),
                ["Jwt:Issuer"] = AuthTokenFactory.Issuer,
                ["Jwt:Audience"] = AuthTokenFactory.Audience,
                ["Jwt:SigningKey"] = AuthTokenFactory.SigningKey,
                ["Jwt:AccessTokenLifetimeMinutes"] = "30"
            });
        });
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = base.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthTokenFactory.Create("admin", "admin"));
        return client;
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

internal static class AuthTokenFactory
{
    public const string Issuer = "BookstoreAuth";
    public const string Audience = "BookstoreClient";
    public const string SigningKey = "EsTe-clav3-HMAc-Sha256-Para-La-Fase12-Bookstor3-microservices-2026!!";

    public static string Create(string subject, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Claims = new Dictionary<string, object>
            {
                ["sub"] = subject,
                ["role"] = role
            },
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}