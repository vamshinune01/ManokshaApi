using System.ComponentModel.DataAnnotations;

public class ProductRequest
{
    [Required, MaxLength(255)] public string Name { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Category { get; set; } = string.Empty;
    [Required, Range(0.01, double.MaxValue)] public decimal Price { get; set; }
    [Required, Range(0, int.MaxValue)] public int Stock { get; set; }
    public bool IsActive { get; set; } = true;
}