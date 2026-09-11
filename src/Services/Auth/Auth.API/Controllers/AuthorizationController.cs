using Auth.API.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace Auth.API.Controllers;

[ApiController]
public class AuthorizationController(AuthorizationService authService) : ControllerBase
{
    [HttpGet("~/connect/authorize")]
    public IActionResult Authorize()
    {
        var request = GetRequest();
        return Content(authService.BuildLoginPage(request), "text/html");
    }

    [HttpPost("~/connect/authorize")]
    public async Task<IActionResult> Login()
    {
        var request = GetRequest();

        var user = authService.FindUser(
            HttpContext.Request.Form["username"].ToString(),
            HttpContext.Request.Form["password"].ToString());

        if (user is null)
        {
            return Content(authService.BuildLoginPage(request, "Invalid username or password."), "text/html");
        }

        var principal = await authService.BuildAuthorizationPrincipalAsync(request, user);

        return principal is null
            ? Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)
            : SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token"), Produces("application/json")]
    public async Task<ActionResult> Exchange()
    {
        var request = GetRequest();

        if (request.IsPasswordGrantType())
        {
            var user = authService.FindUser(
                request.Username ?? string.Empty,
                request.Password ?? string.Empty);

            return user is null
                ? ForbidInvalidGrant("The specified credentials are invalid.")
                : SignIn(authService.BuildPasswordPrincipal(user, request),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var principal = (await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

            return principal is null
                ? ForbidInvalidGrant(request.IsAuthorizationCodeGrantType()
                    ? "The authorization code is no longer valid."
                    : "The refresh token is no longer valid.")
                : SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new NotImplementedException("The specified grant type is not implemented");
    }

    private OpenIddictRequest GetRequest() =>
        HttpContext.GetOpenIddictServerRequest() ??
        throw new InvalidOperationException("The OpenID Connect request cannot be retrieved");

    private ForbidResult ForbidInvalidGrant(string description) =>
        Forbid(properties: new AuthenticationProperties(new Dictionary<string, string?>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        }), authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
}