using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ManokshaApi.Models
{
    // ---------------------------
    // Child Entity: OrderItem
    // ---------------------------
    public class OrderItem
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid OrderId { get; set; }   // Foreign Key

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [Required]
        public Guid ProductId { get; set; }

        [Required]
        [MaxLength(255)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }
    }

    // ---------------------------
    // Parent Entity: Order
    // ---------------------------
    public class Order
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        // 🔗 Relationship
        public List<OrderItem> Items { get; set; } = new();

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // 💳 Payment
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "UPI"; // UPI, Razorpay, Card, COD

        [MaxLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending / Paid / Failed

        // 🚚 Order Tracking
        [MaxLength(50)]
        public string OrderStatus { get; set; } = "Placed"; // Placed, Dispatched, Delivered, etc.

        public string? TrackingNumber { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 📦 Shipping Details
        [Required, MaxLength(100)]
        public string ShippingName { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string ShippingMobile { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string AddressLine1 { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? AddressLine2 { get; set; }

        [Required, MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string State { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string Pincode { get; set; } = string.Empty;
    }
}
