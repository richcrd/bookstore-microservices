using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;

namespace SharedKernel.Security;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddIdpAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenIddict()
            .AddValidation(options =>
            {
                options.SetIssuer(configuration["OpenIddict:Issuer"]);
                options.UseSystemNetHttp();
                options.UseAspNetCore();
            });

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        services.AddAuthorization();

        return services;
    }
}