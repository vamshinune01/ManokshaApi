public class OrderResponse
{
    public Guid Id { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = "Pending";
    public string OrderStatus { get; set; } = "Placed";
    public string? TrackingNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ShippingName { get; set; } = string.Empty;
    public string ShippingMobile { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
}