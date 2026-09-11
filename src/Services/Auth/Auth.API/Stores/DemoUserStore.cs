using Auth.API.Contracts;

namespace Auth.API.Stores;

public sealed class DemoUserStore(IConfiguration configuration)
{
    public DemoUser? FindByUsernameAndPassword(string username, string password) =>
        GetUsers().FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && u.Password == password);

    private List<DemoUser> GetUsers() => configuration.GetSection("AuthUsers:Users").Get<List<DemoUser>>() ?? [];
}