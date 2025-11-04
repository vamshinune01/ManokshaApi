using ManokshaApi.Data;
using ManokshaApi.Models;
using ManokshaApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManokshaApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;

        public UserController(AppDbContext db, IEmailService email) { _db = db; _email = email; }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Mobile))
                return BadRequest("Missing required fields.");

            if (await _db.Users.AnyAsync(u => u.Email == user.Email))
                return BadRequest("Email already registered.");

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var confirmLink = $"{Request.Scheme}://{Request.Host}/api/users/confirm/{user.Id}";
            var body = $"Hi {user.Name},<br/>Please confirm your email by clicking <a href=\"{confirmLink}\">here</a>.";
            await _email.SendEmailAsync(user.Email, "Confirm your email - Manoksha Collections", body);

            return Ok(new { message = "Registered, confirmation email sent.", userId = user.Id });
        }

        [HttpGet("confirm/{id:guid}")]
        public async Task<IActionResult> ConfirmEmail(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsEmailConfirmed = true;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Email confirmed." });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var u = await _db.Users.FindAsync(id);
            if (u == null) return NotFound();
            return Ok(u);
        }
    }
}
