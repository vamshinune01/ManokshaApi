using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ManokshaApi.DTO
{
    public class ProductUploadRequest
    {
        [Required, MaxLength(255)] public string Name { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string Category { get; set; } = string.Empty;
        [Required, Range(0.01, double.MaxValue)] public decimal Price { get; set; }
        [Required, Range(0, int.MaxValue)] public int Stock { get; set; }
        public bool IsActive { get; set; } = true;
        [Required] public IFormFile File { get; set; } = default!;
    }
}
