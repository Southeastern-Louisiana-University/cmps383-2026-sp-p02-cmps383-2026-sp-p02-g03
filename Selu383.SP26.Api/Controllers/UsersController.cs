using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Selu383.SP26.Api.Data;

namespace Selu383.SP26.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly DataContext _db;

    public UsersController(DataContext db) => _db = db;

    public class CreateUserDto
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? RoleName { get; set; }
    }

    // ✅ Must be admin
    [Authorize(Roles = "admin")]
    [HttpPost]
    public IActionResult Create([FromBody] CreateUserDto dto)
    {
        if (dto == null) return BadRequest();

        var username = dto.Username?.Trim();
        var password = dto.Password;
        var roleName = dto.RoleName?.Trim();

        // Empty username
        if (string.IsNullOrWhiteSpace(username)) return BadRequest();

        // No password
        if (string.IsNullOrWhiteSpace(password)) return BadRequest();

        // Empty role
        if (string.IsNullOrWhiteSpace(roleName)) return BadRequest();

        // Role must exist
        if (!_db.Roles.Any(r => r.Name == roleName)) return BadRequest();

        // Duplicate username
        if (_db.Users.Any(u => u.Username == username)) return BadRequest();

        // Bad password (common rule: at least 6 chars)
        if (password.Length < 6) return BadRequest();

        var user = new User
        {
            Username = username,
            Password = password,
            RoleName = roleName
        };

        _db.Users.Add(user);
        _db.SaveChanges();

        return Created($"/api/users/{user.Id}", new
        {
            user.Id,
            user.Username,
            Role = user.RoleName
        });
    }
}
