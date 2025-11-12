using ManokshaApi.Data;
using ManokshaApi.Models;
using ManokshaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ManokshaApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;
        private readonly ISmsService _sms;
        private readonly ILogger<UserController> _logger;
        private readonly IConfiguration _config;

        public UserController(AppDbContext db, IEmailService email, ISmsService sms,
            ILogger<UserController> logger, IConfiguration config)
        {
            _db = db;
            _email = email;
            _sms = sms;
            _logger = logger;
            _config = config;
        }

        // -------------------
        // 1️⃣ Register Customer
        // -------------------
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (string.IsNullOrWhiteSpace(user.Mobile)) return BadRequest("Mobile required");
            if (await _db.Users.AnyAsync(u => u.Mobile == user.Mobile)) return BadRequest("Mobile exists");
            if (!string.IsNullOrWhiteSpace(user.Email) && await _db.Users.AnyAsync(u => u.Email == user.Email))
                return BadRequest("Email exists");

            user.Id = Guid.NewGuid();
            user.Role = "Customer";
            user.IsEmailConfirmed = false;
            user.Otp = GenerateOtp();
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await _sms.SendSmsAsync(user.Mobile, $"Your OTP: {user.Otp}");
            if (!string.IsNullOrEmpty(user.Email))
                await _email.SendEmailAsync(user.Email, "OTP Verification", $"Your OTP is {user.Otp}");

            return Ok(new { message = "Registered. OTP sent.", userId = user.Id });
        }

        // -------------------
        // 2️⃣ Verify OTP & Generate JWT + Refresh Token
        // -------------------
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Mobile == request.Mobile);
            if (user == null) return NotFound("User not found");
            if (user.Otp != request.Otp || user.OtpExpiry < DateTime.UtcNow)
                return BadRequest("Invalid or expired OTP");

            user.IsEmailConfirmed = true;
            user.Otp = null;

            // Generate JWT + Refresh
            var token = GenerateJwtToken(user);
            user.RefreshToken = GenerateRefreshToken();
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "OTP verified",
                token,
                refreshToken = user.RefreshToken,
                role = user.Role
            });
        }

        // -------------------
        // 3️⃣ Refresh Token
        // -------------------
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);
            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                return Unauthorized("Invalid refresh token");

            var token = GenerateJwtToken(user);
            user.RefreshToken = GenerateRefreshToken();
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync();

            return Ok(new { token, refreshToken = user.RefreshToken });
        }

        // -------------------
        // 4️⃣ Login (OTP only)
        // -------------------
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Mobile == request.Mobile);
            if (user == null) return NotFound("User not registered");

            user.Otp = GenerateOtp();
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            await _db.SaveChangesAsync();

            await _sms.SendSmsAsync(user.Mobile, $"Login OTP: {user.Otp}");
            if (!string.IsNullOrEmpty(user.Email))
                await _email.SendEmailAsync(user.Email, "Login OTP", $"Your login OTP: {user.Otp}");

            return Ok(new { message = "OTP sent", userId = user.Id });
        }

        // -------------------
        // 5️⃣ Forgot Password
        // -------------------
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Mobile == request.Mobile || u.Email == request.Email);
            if (user == null) return NotFound("User not found");

            user.Otp = GenerateOtp();
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            await _db.SaveChangesAsync();

            await _sms.SendSmsAsync(user.Mobile, $"Reset OTP: {user.Otp}");
            if (!string.IsNullOrEmpty(user.Email))
                await _email.SendEmailAsync(user.Email, "Password Reset OTP", $"Your OTP: {user.Otp}");

            return Ok(new { message = "OTP sent" });
        }

        // -------------------
        // 6️⃣ SuperAdmin: Add Worker
        // -------------------
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("add-worker")]
        public async Task<IActionResult> AddWorker([FromBody] User worker)
        {
            if (await _db.Users.AnyAsync(u => u.Mobile == worker.Mobile))
                return BadRequest("Mobile exists");
            if (!string.IsNullOrWhiteSpace(worker.Email) && await _db.Users.AnyAsync(u => u.Email == worker.Email))
                return BadRequest("Email exists");

            worker.Id = Guid.NewGuid();
            worker.Role = "Worker";
            worker.IsEmailConfirmed = true;
            _db.Users.Add(worker);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Worker added", userId = worker.Id });
        }

        // -------------------
        // 7️⃣ SuperAdmin: Remove Worker
        // -------------------
        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("remove-worker/{id:guid}")]
        public async Task<IActionResult> RemoveWorker(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null || user.Role != "Worker") return NotFound("Worker not found");

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Worker removed" });
        }

        // -------------------
        // Helper Methods
        // -------------------
        private string GenerateOtp()
        {
            var random = RandomNumberGenerator.GetInt32(100000, 999999);
            return random.ToString();
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? throw new Exception("JWT_SECRET_KEY not set");
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.MobilePhone, user.Mobile),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role)
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            var random = new byte[64];
            RandomNumberGenerator.Fill(random);
            return Convert.ToBase64String(random);
        }
    }

    // -------------------
    // Request DTOs
    // -------------------
    public record VerifyOtpRequest(string Mobile, string Otp);
    public record LoginRequest(string Mobile);
    public record RefreshTokenRequest(string RefreshToken);
    public record ForgotPasswordRequest(string Mobile, string? Email);
}
