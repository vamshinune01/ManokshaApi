using System;
using System.ComponentModel.DataAnnotations;

namespace ManokshaApi.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Mobile { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        public bool IsEmailConfirmed { get; set; } = false;

        // 🔐 OTP Verification Fields
        public string? Otp { get; set; }
        public DateTime? OtpExpiry { get; set; }

        // 👥 Role-Based Access (Customer, Worker, SuperAdmin)
        [Required]
        public string Role { get; set; } = "Customer"; // Default = Customer

        // 🚫 Account status
        public bool IsActive { get; set; } = true;

        // 🔁 Refresh Token Support
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
    }
}
