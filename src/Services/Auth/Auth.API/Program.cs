using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Security;
using SharedKernel.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddServiceTelemetry(builder.Configuration, "Auth.API");

var app = builder.Build();

app.UseServiceTelemetry();

app.MapPost("/api/v1/auth/token", (LoginRequest request) => TokenHandler.Issue(app.Configuration, request));

app.MapHealthChecks("/health");

app.Run();

public static class TokenHandler
{
    public static IResult Issue(IConfiguration configuration, LoginRequest request)
    {
        var user = configuration.GetSection("AuthUsers:Users").Get<List<DemoUser>>()
            ?.FirstOrDefault(u =>
                u.Username.Equals(request.Username, StringComparison.OrdinalIgnoreCase)
                && u.Password == request.Password);

        if (user is null)
        {
            return Results.Json(new { error = "Invalid username or password." },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var jwt = new JwtOptions();
        configuration.GetSection(JwtOptions.SectionName).Bind(jwt);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = user.Username,
            [JwtRegisteredClaimNames.Name] = user.Name,
            ["role"] = user.Role,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString()
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Claims = claims,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(jwt.AccessTokenLifetimeMinutes),
            SigningCredentials = credentials
        };

        var accessToken = new JsonWebTokenHandler().CreateToken(descriptor);

        return Results.Ok(new TokenResponse(
            accessToken,
            jwt.AccessTokenLifetimeMinutes,
            user.Username,
            user.Name,
            user.Role));
    }
}

public record LoginRequest(string Username, string Password);
public record DemoUser(string Username, string Password, string Name, string Role);
public record TokenResponse(string Token, int ExpiresIn, string Username, string DisplayName, string Role);