using ManokshaApi.Data;
using ManokshaApi.Models;
using ManokshaApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ManokshaApi.Controllers
{
    using MyLoginRequest = ManokshaApi.DTO.LoginRequest;
    using MyVerifyOtpRequest = ManokshaApi.DTO.VerifyOtpRequest;
    using ForgotPasswordRequest = ManokshaApi.DTO.ForgotPasswordRequest;

    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;
        private readonly ISmsService _sms;
        private readonly ILogger<UserController> _logger;

        public UserController(AppDbContext db, IEmailService email, ISmsService sms, ILogger<UserController> logger)
        {
            _db = db;
            _email = email;
            _sms = sms;
            _logger = logger;
        }

        // ✅ Register new user
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            if (string.IsNullOrWhiteSpace(user.Mobile))
                return BadRequest("Mobile number is required.");

            if (await _db.Users.AnyAsync(u => u.Mobile == user.Mobile))
                return BadRequest("Mobile number already registered.");

            if (!string.IsNullOrWhiteSpace(user.Email) &&
                await _db.Users.AnyAsync(u => u.Email == user.Email))
                return BadRequest("Email already registered.");

            user.Id = Guid.NewGuid();
            user.IsEmailConfirmed = false;
            user.Otp = GenerateOtp();
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Send OTP
            await _sms.SendSmsAsync(user.Mobile, $"Your Manoksha verification code is {user.Otp}");
            if (!string.IsNullOrEmpty(user.Email))
                await _email.SendEmailAsync(user.Email, "OTP Verification - Manoksha Collections", $"Your OTP is {user.Otp}");

            _logger.LogInformation("✅ New user registered with Mobile: {Mobile}", user.Mobile);
            return Ok(new { message = "User registered. OTP sent to mobile and email.", userId = user.Id });
        }

        // ✅ Verify OTP (for registration or login)
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] MyVerifyOtpRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Mobile == request.Mobile);

            if (user == null)
            {
                _logger.LogWarning("❌ OTP verification failed: User not found for {Mobile}", request.Mobile);
                return NotFound("User not found.");
            }

            if (user.Otp != request.Otp || user.OtpExpiry < DateTime.UtcNow)
            {
                _logger.LogWarning("❌ Invalid or expired OTP for {Mobile}", request.Mobile);
                return BadRequest("Invalid or expired OTP.");
            }

            user.IsEmailConfirmed = true;
            user.Otp = null;
            await _db.SaveChangesAsync();

            _logger.LogInformation("✅ OTP verified for {Mobile}", request.Mobile);
            return Ok(new { message = "OTP verified successfully.", userId = user.Id });
        }

        // ✅ Login (resend OTP for existing users)
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] MyLoginRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Mobile == request.Mobile);
            if (user == null)
            {
                _logger.LogWarning("❌ Login attempt for unregistered mobile {Mobile}", request.Mobile);
                return NotFound("User not registered.");
            }

            user.Otp = GenerateOtp();
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            await _db.SaveChangesAsync();

            await _sms.SendSmsAsync(user.Mobile, $"Your login OTP is {user.Otp}");
            if (!string.IsNullOrEmpty(user.Email))
                await _email.SendEmailAsync(user.Email, "Login OTP - Manoksha Collections", $"Your login OTP is {user.Otp}");

            _logger.LogInformation("✅ Login OTP sent to {Mobile}", request.Mobile);
            return Ok(new { message = "OTP sent for login verification.", userId = user.Id });
        }

        // ✅ Forgot Password (same OTP flow)
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Mobile == request.Mobile || u.Email == request.Email);
            if (user == null)
            {
                _logger.LogWarning("❌ Forgot password failed: No user for {Mobile}/{Email}", request.Mobile, request.Email);
                return NotFound("User not found.");
            }

            user.Otp = GenerateOtp();
            user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
            await _db.SaveChangesAsync();

            await _sms.SendSmsAsync(user.Mobile, $"Your password reset OTP is {user.Otp}");
            if (!string.IsNullOrEmpty(user.Email))
                await _email.SendEmailAsync(user.Email, "Password Reset - Manoksha Collections", $"Your OTP to reset password is {user.Otp}");

            _logger.LogInformation("✅ Forgot password OTP sent to {Mobile}", request.Mobile);
            return Ok(new { message = "OTP sent for password reset.", userId = user.Id });
        }

        // ✅ Helper: Generate Random OTP
        private string GenerateOtp()
        {
            var random = RandomNumberGenerator.GetInt32(100000, 999999);
            return random.ToString();
        }
    }
}
