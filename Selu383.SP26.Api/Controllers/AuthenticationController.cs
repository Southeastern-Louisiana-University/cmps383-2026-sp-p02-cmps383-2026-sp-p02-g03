using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Selu383.SP26.Api.Data;

namespace Selu383.SP26.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    private readonly DataContext db;

    public AuthenticationController(DataContext db)
    {
        this.db = db;
    }

    public sealed class LoginDto
    {
        public string? UserName { get; set; }
        public string? Password { get; set; }
    }

    public sealed class UserDto
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string[]? Roles { get; set; }
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest();

        // NOTE: adjust property names if yours differ (Username vs UserName, etc.)
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.UserName);
        if (user == null) return BadRequest();

        // tests assume simple password check works
        if (user.Password != dto.Password) return BadRequest();

        // RoleName is what you seeded in Program.cs ("User"/"Admin")
        var roles = new[] { user.RoleName };

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
        };
        foreach (var r in roles.Where(r => !string.IsNullOrWhiteSpace(r)))
            claims.Add(new Claim(ClaimTypes.Role, r));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // THIS is what causes Set-Cookie to be returned
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Ok(new UserDto
        {
            Id = user.Id,
            UserName = user.Username,
            Roles = roles
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var userId)) return Unauthorized();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return Unauthorized();

        return Ok(new UserDto
        {
            Id = user.Id,
            UserName = user.Username,
            Roles = new[] { user.RoleName }
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        // THIS is what clears cookie and causes Set-Cookie in response
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}