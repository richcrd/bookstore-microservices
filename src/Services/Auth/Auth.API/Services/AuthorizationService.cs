using System.Security.Claims;
using System.Text.Encodings.Web;
using Auth.API.Contracts;
using Auth.API.Stores;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;


namespace Auth.API.Services;

public sealed class AuthorizationService(
    DemoUserStore userStore,
    IOpenIddictApplicationManager applications,
    IOpenIddictAuthorizationManager authorizations)
{
    public DemoUser? FindUser(string username, string password) =>
        userStore.FindByUsernameAndPassword(username, password);

    public string BuildLoginPage(OpenIddictRequest request, string? error = null)
    {
        var hiddenFields = string.Concat(request.GetParameters()
            .Where(p => p.Key is not ("username" or "password"))
            .Select(p =>
                $"<input type=\"hidden\" name=\"{HtmlEncoder.Default.Encode(p.Key)}\" value=\"{HtmlEncoder.Default.Encode(p.Value.ToString())}\" />"));

        var errorHtml = error is null ? string.Empty : $"<p class=\"error\">{HtmlEncoder.Default.Encode(error)}</p>";

        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>BookStore · Sign in</title>
              <style>
                body { font-family: system-ui, sans-serif; background: #f4f4f5; display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; }
                .card { background: #fff; padding: 2rem; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,.12); width: 100%; max-width: 340px; }
                h1 { font-size: 1.25rem; margin: 0 0 1rem; }
                label { display: block; margin: .75rem 0 .25rem; font-size: .875rem; }
                input[type=text], input[type=password] { width: 100%; padding: .5rem; border: 1px solid #d4d4d8; border-radius: 4px; box-sizing: border-box; }
                button { width: 100%; margin-top: 1rem; padding: .6rem; background: #2563eb; color: #fff; border: 0; border-radius: 4px; cursor: pointer; }
                .error { color: #dc2626; font-size: .875rem; }
              </style>
            </head>
            <body>
              <form class="card" method="post" action="/connect/authorize">
                {{hiddenFields}}
                <h1>BookStore · Sign in</h1>
                {{errorHtml}}
                <label for="username">Username</label>
                <input id="username" name="username" type="text" autofocus required>
                <label for="password">Password</label>
                <input id="password" name="password" type="password" required>
                <button type="submit">Sign in</button>
              </form>
            </body>
            </html>
            """;
    }

    public ClaimsPrincipal BuildPasswordPrincipal(DemoUser user, OpenIddictRequest request)
    {
        var identity = new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            OpenIddictConstants.Claims.Name,
            OpenIddictConstants.Claims.Role);

        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Username));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, user.Name));
        identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, user.Role));

        identity.SetDestinations(static claim => claim.Type switch
        {
            OpenIddictConstants.Claims.Role =>
                [OpenIddictConstants.Destinations.AccessToken],
            _ => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken]
        });

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        return principal;
    }

    public async Task<ClaimsPrincipal?> BuildAuthorizationPrincipalAsync(OpenIddictRequest request, DemoUser user)
    {
        var application = await applications.FindByClientIdAsync(request.ClientId);
        if (application is null ||
            !await applications.HasConsentTypeAsync(application, OpenIddictConstants.ConsentTypes.Implicit))
        {
            return null;
        }

        var identity = new ClaimsIdentity(
            claims: [
                new Claim(OpenIddictConstants.Claims.Subject, user.Username),
                new Claim(OpenIddictConstants.Claims.Name, user.Name),
                new Claim(OpenIddictConstants.Claims.Role, user.Role)
            ],
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: OpenIddictConstants.Claims.Name,
            roleType: OpenIddictConstants.Claims.Role);

        identity.SetDestinations(GetDestinations);

        var authorization = await authorizations.CreateAsync(
            principal: new ClaimsPrincipal(identity),
            subject: user.Username,
            client: await applications.GetIdAsync(application),
            type: OpenIddictConstants.AuthorizationTypes.Permanent,
            scopes: request.GetScopes());

        identity.SetAuthorizationId(await authorizations.GetIdAsync(authorization));

        return new ClaimsPrincipal(identity);
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case OpenIddictConstants.Claims.Name:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (claim.Subject.HasScope(OpenIddictConstants.Scopes.Profile))
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                yield break;

            case OpenIddictConstants.Claims.Role:
                yield return OpenIddictConstants.Destinations.AccessToken;
                if (claim.Subject.HasScope(OpenIddictConstants.Scopes.Roles))
                {
                    yield return OpenIddictConstants.Destinations.IdentityToken;
                }
                yield break;

            default:
                yield return OpenIddictConstants.Destinations.AccessToken;
                yield break;
        }
    }
}