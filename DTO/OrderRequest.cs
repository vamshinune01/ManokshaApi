using System.ComponentModel.DataAnnotations;

public class OrderRequest
{
    [Required] public List<OrderItemRequest> Items { get; set; } = new();
    [Required, MaxLength(100)] public string ShippingName { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string ShippingMobile { get; set; } = string.Empty;
    [Required, MaxLength(255)] public string AddressLine1 { get; set; } = string.Empty;
    [MaxLength(255)] public string? AddressLine2 { get; set; }
    [Required, MaxLength(100)] public string City { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string State { get; set; } = string.Empty;
    [Required, MaxLength(10)] public string Pincode { get; set; } = string.Empty;
    [MaxLength(50)] public string PaymentMethod { get; set; } = "UPI";
}