using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ManokshaApi.Models
{
    public class OrderItem
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class Order
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public List<OrderItem> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; } = "UPI"; // UPI, Razorpay, Card, CashOnDelivery
        public string PaymentStatus { get; set; } = "Pending"; // Pending / Paid / Failed
        public string OrderStatus { get; set; } = "Placed"; // Placed, Dispatched, Delivered, ReturnRequested, Returned
        public string? TrackingNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Shipping details (India)
        public string ShippingName { get; set; } = string.Empty;
        public string ShippingMobile { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string? AddressLine2 { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
    }
}
