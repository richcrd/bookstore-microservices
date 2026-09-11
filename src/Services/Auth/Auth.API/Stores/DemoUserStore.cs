using Auth.API.Contracts;

namespace Auth.API.Stores;

public sealed class DemoUserStore(IConfiguration configuration)
{
    public DemoUser? FindByUsernameAndPassword(string username, string password) =>
        FindByUsername(username) is { } user && user.Password == password ? user : null;
    
    public DemoUser? FindByUsername(string username) =>
        GetUsers().FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    private List<DemoUser> GetUsers() => configuration.GetSection("AuthUsers:Users").Get<List<DemoUser>>() ?? [];
}