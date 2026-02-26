using Microsoft.EntityFrameworkCore;
using Selu383.SP26.Api.Features.Locations;

namespace Selu383.SP26.Api.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<Location> Locations { get; set; } = default!;

    // Auth tables (must exist in your model if you're seeding/logging in)
    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Role> Roles { get; set; } = default!;

    // ✅ Tests expect these to be SAFE (return bool, never throw)
    public bool HasLocation(string name) => Locations.Any(l => l.Name == name);
    public bool HasUser(string username) => Users.Any(u => u.Username == username);
    public bool HasRole(string roleName) => Roles.Any(r => r.Name == roleName);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // apply IEntityTypeConfiguration<> from this assembly (if you have any)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}
