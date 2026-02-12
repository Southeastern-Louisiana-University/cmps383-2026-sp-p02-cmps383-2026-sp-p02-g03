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
    private readonly DataContext _db;

    public AuthenticationController(DataContext db) => _db = db;

    public class LoginDto
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto? dto)
    {
        // Try JSON first
        var username = dto?.Username;
        var password = dto?.Password;

        // Try form data (x-www-form-urlencoded / multipart)
        if ((string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) && Request.HasFormContentType)
        {
            username =
                Request.Form["username"].FirstOrDefault() ??
                Request.Form["Username"].FirstOrDefault() ??
                Request.Form["userName"].FirstOrDefault() ??
                Request.Form["UserName"].FirstOrDefault();

            password =
                Request.Form["password"].FirstOrDefault() ??
                Request.Form["Password"].FirstOrDefault();
        }

        // Try querystring (cheap to support; sometimes test helpers do this)
        if (string.IsNullOrWhiteSpace(username))
            username = Request.Query["username"].FirstOrDefault() ?? Request.Query["Username"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(password))
            password = Request.Query["password"].FirstOrDefault() ?? Request.Query["Password"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return BadRequest();

        username = username.Trim();

        // Case-insensitive username match (avoids "Bob" vs "bob" issues)
        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

        if (user == null) return BadRequest();

        // Exact password match
        if (user.Password != password) return BadRequest();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.RoleName ?? "")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Ok(new { user.Username, Role = user.RoleName });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            Username = User.Identity?.Name,
            Role = User.FindFirstValue(ClaimTypes.Role)
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok();
    }
}

