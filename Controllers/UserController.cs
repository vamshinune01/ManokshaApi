using ManokshaApi.Data;
using ManokshaApi.Models;
using ManokshaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ManokshaApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;
        private readonly ISmsService _sms;
        private readonly JwtService _jwt;
        private readonly ILogger<UserController> _logger;

        public UserController(AppDbContext db, IEmailService email, ISmsService sms, JwtService jwt, ILogger<UserController> logger)
        {
            _db = db;
            _email = email;
            _sms = sms;
            _jwt = jwt;
            _logger = logger;
        }

        // ✅ Register Customer
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (await _db.Users.AnyAsync(u => u.Mobile == user.Mobile))
                return BadRequest("Mobile number already registered.");

            user.Id = Guid.NewGuid();
            user.Role = "Customer";
            user.IsActive = true;
            user.Otp = GenerateOtp();
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await _sms.SendSmsAsync(user.Mobile, $"Your OTP is {user.Otp}");
            if (!string.IsNullOrEmpty(user.Email))
                await _email.SendEmailAsync(user.Email, "OTP Verification", $"Your OTP is {user.Otp}");

            return Ok(new { message = "Registered successfully. OTP sent." });
        }

        // ✅ Verify OTP and return JWT
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] DTO.VerifyOtpRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Mobile == request.Mobile);
            if (user == null) return NotFound("User not found.");
            if (!user.IsActive) return BadRequest("Account deactivated.");

            if (user.Otp != request.Otp || user.OtpExpiry < DateTime.UtcNow)
                return BadRequest("Invalid or expired OTP.");

            user.Otp = null;
            user.IsEmailConfirmed = true;

            var accessToken = _jwt.GenerateToken(user);
            var refreshToken = _jwt.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            await _db.SaveChangesAsync();

            return Ok(new { accessToken, refreshToken, role = user.Role });
        }

        // ✅ Refresh token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                return Unauthorized("Invalid or expired refresh token.");

            var newAccessToken = _jwt.GenerateToken(user);
            var newRefreshToken = _jwt.GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync();

            return Ok(new { accessToken = newAccessToken, refreshToken = newRefreshToken });
        }

        // ✅ SuperAdmin: Add new Worker
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("add-worker")]
        public async Task<IActionResult> AddWorker([FromBody] User worker)
        {
            if (await _db.Users.AnyAsync(u => u.Mobile == worker.Mobile))
                return BadRequest("Worker mobile already registered.");

            worker.Id = Guid.NewGuid();
            worker.Role = "Worker";
            worker.IsActive = true;
            _db.Users.Add(worker);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Worker added successfully." });
        }

        // ✅ SuperAdmin: Remove Worker
        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("remove-worker/{mobile}")]
        public async Task<IActionResult> RemoveWorker(string mobile)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Mobile == mobile && u.Role == "Worker");
            if (user == null) return NotFound("Worker not found.");

            user.IsActive = false;
            user.RefreshToken = null;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Worker removed and access revoked." });
        }

        // ✅ Helper: Generate OTP
        private string GenerateOtp() =>
            RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }
}
