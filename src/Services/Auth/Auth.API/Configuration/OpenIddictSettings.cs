namespace Auth.API.Configuration;

public sealed class OpenIddictSettings
{
    public const string SectionName = "OpenIddict";
    
    public string Issuer { get; set; } = "http://localhost:5080";
    public string SpaClientId { get; set; } = "web-spa";
    public string SpaRedirectUri { get; set; } = "http://localhost:5173/callback";
    public string SpaPostLogoutRedirectUri { get; set; } = "http://localhost:5173/";
    public string CliClientId { get; set; } = "cli";
    public string CliClientSecret { get; set; } = "cli-dev-secret";
}