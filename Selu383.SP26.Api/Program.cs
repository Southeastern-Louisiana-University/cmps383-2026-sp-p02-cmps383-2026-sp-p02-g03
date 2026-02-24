using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Selu383.SP26.Api.Data;
using Selu383.SP26.Api.Features.Locations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DataContext")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Authentication (Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "auth";

        // IMPORTANT for tests: they run on http://localhost (not https)
        options.Cookie.SecurePolicy = CookieSecurePolicy.None;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.HttpOnly = true;

        // API tests expect status codes, not redirects
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataContext>();

    // Use EnsureCreated for test compatibility (in-memory / swapped providers)
    db.Database.EnsureCreated();

    // ---- Roles (match what tests expect: usually "Admin" and "User") ----
    if (!db.Roles.Any(r => r.Name == "Admin"))
        db.Roles.Add(new Role { Name = "Admin" });

    if (!db.Roles.Any(r => r.Name == "User"))
        db.Roles.Add(new Role { Name = "User" });

    db.SaveChanges();

    // ---- Users (match tests: Password123!) ----
    const string seededPassword = "Password123!";

    // bob (User)
    var bobs = db.Users.Where(u => u.Username == "bob").ToList();
    if (bobs.Count == 0)
    {
        db.Users.Add(new User
        {
            Username = "bob",
            Password = seededPassword,
            RoleName = "User"
        });
    }
    else
    {
        var keep = bobs[0];
        keep.Password = seededPassword;
        keep.RoleName = "User";

        if (bobs.Count > 1)
            db.Users.RemoveRange(bobs.Skip(1));
    }

    // sue (User)  <-- tests commonly use this user for "wrong user" scenarios
    var sues = db.Users.Where(u => u.Username == "sue").ToList();
    if (sues.Count == 0)
    {
        db.Users.Add(new User
        {
            Username = "sue",
            Password = seededPassword,
            RoleName = "User"
        });
    }
    else
    {
        var keep = sues[0];
        keep.Password = seededPassword;
        keep.RoleName = "User";

        if (sues.Count > 1)
            db.Users.RemoveRange(sues.Skip(1));
    }

    // galkadi (Admin)
    var admins = db.Users.Where(u => u.Username == "galkadi").ToList();
    if (admins.Count == 0)
    {
        db.Users.Add(new User
        {
            Username = "galkadi",
            Password = seededPassword,
            RoleName = "Admin"
        });
    }
    else
    {
        var keep = admins[0];
        keep.Password = seededPassword;
        keep.RoleName = "Admin";

        if (admins.Count > 1)
            db.Users.RemoveRange(admins.Skip(1));
    }

    db.SaveChanges();

    // ---- Locations (must have at least 3 seeded) ----
    if (!db.Locations.Any())
    {
        db.Locations.AddRange(
            new Location { Name = "Location 1", Address = "123 Main St", TableCount = 10 },
            new Location { Name = "Location 2", Address = "456 Oak Ave", TableCount = 20 },
            new Location { Name = "Location 3", Address = "789 Pine Ln", TableCount = 15 }
        );
        db.SaveChanges();
    }
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// DO NOT use HTTPS redirection for these tests (it causes 303 redirects)
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }