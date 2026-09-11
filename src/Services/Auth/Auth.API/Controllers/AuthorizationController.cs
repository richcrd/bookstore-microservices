using System.Security.Claims;
using Auth.API.Stores;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Auth.API.Controllers;

[ApiController]
public class AuthorizationController(DemoUserStore userStore) : ControllerBase
{
    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<ActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
                      throw new InvalidOperationException("The OpenID Connect request cannot be retrieved");

        if (request.IsPasswordGrantType())
        {
            var user = userStore.FindByUsernameAndPassword(
                request.Username ?? string.Empty,
                request.Password ?? string.Empty);

            if (user is null)
            {
                return Forbid(properties: new AuthenticationProperties(new Dictionary<string, string>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The specified credentials are invalid."
                }!), authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var identity = new ClaimsIdentity(
                TokenValidationParameters.DefaultAuthenticationType,
                OpenIddictConstants.Claims.Name,
                OpenIddictConstants.Claims.Role);

            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, user.Username));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Name, user.Name));
            identity.AddClaim(new Claim(OpenIddictConstants.Claims.Role, user.Role));
            
            identity.SetDestinations(static claim => claim.Type switch
            {
                OpenIddictConstants.Claims.Role => [OpenIddictConstants.Destinations.AccessToken],
                _ => [OpenIddictConstants.Destinations.AccessToken, OpenIddictConstants.Destinations.IdentityToken]
            });

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
            // OpenIddict validated and rotated the refresh token
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            if (result.Principal is null)
            {
                return Forbid(properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The refresh token is no longer valid."
                }), authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }
            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }
        
        throw new NotImplementedException("The specified grant type is not implemented");
    }
}