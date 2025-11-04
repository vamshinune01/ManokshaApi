using System;
using System.ComponentModel.DataAnnotations;

namespace ManokshaApi.Models
{
    public class Product
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        [Required] public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public int Stock { get; set; } = 100;
    }
}
