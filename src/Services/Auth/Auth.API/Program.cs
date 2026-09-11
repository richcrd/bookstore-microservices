using Auth.API.Configuration;
using Auth.API.Data;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using SharedKernel.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddServiceTelemetry(builder.Configuration, "Auth.API");
builder.Services.AddAuthServices(builder.Configuration);

var app = builder.Build();

app.UseServiceTelemetry();

app.MapControllers();
app.MapHealthChecks("/health");

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.Database.MigrateAsync();
    
    var settings = scope.ServiceProvider.GetRequiredService<OpenIddictSettings>();
    var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    var scopes = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

    string[] scopeNames =
    [
        OpenIddictConstants.Scopes.OpenId,
        OpenIddictConstants.Scopes.Profile,
        OpenIddictConstants.Scopes.Email,
        OpenIddictConstants.Scopes.Roles,
        OpenIddictConstants.Scopes.OfflineAccess
    ];

    foreach (var scopeName in scopeNames)
    {
        if (await scopes.FindByNameAsync(scopeName) is null)
        {
            await scopes.CreateAsync(new OpenIddictScopeDescriptor()
            {
                Name = scopeName,
                DisplayName = scopeName,
            });
        }
    }

    // web-spa Authorization Code + PKCE
    if (await applications.FindByClientIdAsync(settings.SpaClientId) is null)
    {
        await applications.CreateAsync(new OpenIddictApplicationDescriptor()
        {
            ClientId = settings.SpaClientId,
            ClientType = OpenIddictConstants.ClientTypes.Public,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "BookStore SPA",
            RedirectUris = { new Uri(settings.SpaRedirectUri) },
            PostLogoutRedirectUris = { new Uri(settings.SpaPostLogoutRedirectUri) },
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Authorization,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                OpenIddictConstants.Permissions.ResponseTypes.Code,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Permissions.Scopes.Profile,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Permissions.Scopes.Email,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Permissions.Scopes.Roles,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess
            },
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        });
    }
    
    // cli Password + Refresh Token
    if (await applications.FindByClientIdAsync(settings.CliClientId) is null)
    {
        await applications.CreateAsync(new OpenIddictApplicationDescriptor
        {
            ClientId = settings.CliClientId,
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            ConsentType = OpenIddictConstants.ConsentTypes.Implicit,
            DisplayName = "BookStore CLI",
            ClientSecret = settings.CliClientSecret,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.Endpoints.EndSession,
                OpenIddictConstants.Permissions.GrantTypes.Password,
                OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OpenId,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Profile,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Email,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.Roles,
                OpenIddictConstants.Permissions.Prefixes.Scope + OpenIddictConstants.Scopes.OfflineAccess
            }
        });
    }
}

app.Run();
