using Auth.API.Data;
using Auth.API.Services;
using Auth.API.Stores;
using Microsoft.EntityFrameworkCore;

namespace Auth.API.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();

        services.AddAuthentication();
        services.AddAuthorization();

        services.AddDbContext<AuthDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("AuthDb"));
            options.UseOpenIddict();
        });

        var settings = configuration.GetSection(OpenIddictSettings.SectionName)
            .Get<OpenIddictSettings>() ?? new OpenIddictSettings();

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<AuthDbContext>();
            })
            .AddServer(options =>
            {
                options.SetIssuer(new Uri(settings.Issuer));

                options.SetAuthorizationEndpointUris("connect/authorize");
                options.SetTokenEndpointUris("connect/token");
                options.SetEndSessionEndpointUris("connect/logout");

                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(30));
                options.SetRefreshTokenLifetime(TimeSpan.FromDays(14));

                options.AllowPasswordFlow();
                options.AllowRefreshTokenFlow();
                options.AllowAuthorizationCodeFlow();
                options.RequireProofKeyForCodeExchange();

                options.AddDevelopmentEncryptionCertificate();
                options.AddDevelopmentSigningCertificate();
                options.DisableAccessTokenEncryption();
                options.UseReferenceRefreshTokens();

                var aspNetCore = options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();

                if (configuration.GetValue<bool>("OpenIddict:DisableTransportSecurityRequirement", false))
                {
                    aspNetCore.DisableTransportSecurityRequirement();
                }
            });

        services.AddSingleton(settings);
        services.AddSingleton<DemoUserStore>();
        services.AddScoped<AuthorizationService>();

        return services;
    }
}