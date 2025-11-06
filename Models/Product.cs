using System;

namespace ManokshaApi.Models
{
    public class Product
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Description { get; set; } // Optional
    }
}
