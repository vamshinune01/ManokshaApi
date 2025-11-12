using System.ComponentModel.DataAnnotations;

public class OrderItemRequest
{
    [Required] public Guid ProductId { get; set; }
    [Required][Range(1, int.MaxValue)] public int Quantity { get; set; }
}