using System;
using System.ComponentModel.DataAnnotations;

namespace ManokshaApi.Models
{
    public class User
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Mobile { get; set; } = string.Empty;
        [Required] public string Email { get; set; } = string.Empty;
        public bool IsEmailConfirmed { get; set; } = false;
    }
}
