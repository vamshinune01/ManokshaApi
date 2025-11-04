using System;
using System.ComponentModel.DataAnnotations;

namespace ManokshaApi.Models
{
    public class ReturnRequest
    {
        [Key] public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OrderId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Requested"; // Requested, Accepted, Refunded
        public string? ReturnTrackingNumber { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}
