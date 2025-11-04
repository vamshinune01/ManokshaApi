using ManokshaApi.Data;
using ManokshaApi.Models;
using ManokshaApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManokshaApi.Controllers
{
    [ApiController, Route("api/orders")]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IPaymentService _payments;
        private readonly IEmailService _email;

        public OrderController(AppDbContext db, IPaymentService payments, IEmailService email)
        {
            _db = db; _payments = payments; _email = email;
        }

        [HttpPost("place")]
        public async Task<IActionResult> PlaceOrder([FromBody] Order order)
        {
            var user = await _db.Users.FindAsync(order.UserId);
            if (user == null) return BadRequest("Invalid user.");

            order.OrderStatus = "Placed";
            order.PaymentStatus = order.PaymentMethod == "CashOnDelivery" ? "Pending" : "Pending";
            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            await _email.SendEmailAsync(user.Email, "Order Placed - Manoksha Collections", $"Thanks {user.Name}, your order {order.Id} is placed.");

            if (order.PaymentMethod != "CashOnDelivery")
            {
                var (ok, data) = await _payments.CreatePaymentOrder(order.TotalAmount, "INR", order.Id.ToString());
                if (ok) return Ok(new { orderId = order.Id, payment = data });
            }

            return Ok(new { orderId = order.Id });
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> OrdersByUser(Guid userId)
        {
            var orders = await _db.Orders.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToListAsync();
            return Ok(orders);
        }

        [HttpPut("{id:guid}/attach-tracking")]
        public async Task<IActionResult> AttachTracking(Guid id, [FromBody] string trackingNumber)
        {
            var o = await _db.Orders.FindAsync(id);
            if (o == null) return NotFound();
            o.TrackingNumber = trackingNumber;
            o.OrderStatus = "Dispatched";
            await _db.SaveChangesAsync();
            var user = await _db.Users.FindAsync(o.UserId);
            if (user != null) await _email.SendEmailAsync(user.Email, "Order Dispatched", $"Your order {o.Id} is dispatched. Tracking: {trackingNumber}");
            return Ok(o);
        }

        [HttpGet("track/{tracking}")]
        public async Task<IActionResult> Track(string tracking)
        {
            var o = await _db.Orders.FirstOrDefaultAsync(x => x.TrackingNumber == tracking);
            if (o == null) return NotFound();
            return Ok(o);
        }

        [HttpPut("{id:guid}/update-status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] string status)
        {
            var o = await _db.Orders.FindAsync(id);
            if (o == null) return NotFound();
            o.OrderStatus = status;
            await _db.SaveChangesAsync();
            return Ok(o);
        }
    }
}
