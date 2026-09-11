using Microsoft.EntityFrameworkCore;

namespace Auth.API.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseOpenIddict();
        base.OnModelCreating(modelBuilder);
    }
}