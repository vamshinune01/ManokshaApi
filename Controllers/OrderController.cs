using ManokshaApi.Data;
using ManokshaApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrderController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize(Roles = "Customer")]
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest request)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        if (request.Items.Count == 0) return BadRequest("Order must have at least 1 item.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ShippingName = request.ShippingName,
            ShippingMobile = request.ShippingMobile,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            State = request.State,
            Pincode = request.Pincode,
            PaymentMethod = request.PaymentMethod,
            CreatedAt = DateTime.UtcNow
        };

        decimal totalAmount = 0;
        foreach (var itemReq in request.Items)
        {
            var product = await _db.Products.FindAsync(itemReq.ProductId);
            if (product == null || !product.IsActive)
                return BadRequest($"Invalid product {itemReq.ProductId}");

            if (itemReq.Quantity <= 0) return BadRequest("Quantity must be at least 1");
            if (itemReq.Quantity > product.Stock) return BadRequest($"Not enough stock for {product.Name}");

            // Reduce stock
            product.Stock -= itemReq.Quantity;

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = itemReq.Quantity,
                UnitPrice = product.Price
            };
            order.Items.Add(orderItem);
            totalAmount += product.Price * itemReq.Quantity;
        }

        order.TotalAmount = totalAmount;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return Ok(new OrderResponse
        {
            Id = order.Id,
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            TrackingNumber = order.TrackingNumber,
            CreatedAt = order.CreatedAt,
            ShippingName = order.ShippingName,
            ShippingMobile = order.ShippingMobile,
            AddressLine1 = order.AddressLine1,
            AddressLine2 = order.AddressLine2,
            City = order.City,
            State = order.State,
            Pincode = order.Pincode,
            Items = order.Items.Select(i => new OrderItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        });
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .ToListAsync();

        return Ok(orders.Select(order => new OrderResponse
        {
            Id = order.Id,
            TotalAmount = order.TotalAmount,
            OrderStatus = order.OrderStatus,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            TrackingNumber = order.TrackingNumber,
            CreatedAt = order.CreatedAt,
            ShippingName = order.ShippingName,
            ShippingMobile = order.ShippingMobile,
            AddressLine1 = order.AddressLine1,
            AddressLine2 = order.AddressLine2,
            City = order.City,
            State = order.State,
            Pincode = order.Pincode,
            Items = order.Items.Select(i => new OrderItemResponse
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        }));
    }

    [Authorize(Roles = "Worker,SuperAdmin")]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromQuery] string status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound("Order not found");

        order.OrderStatus = status;
        await _db.SaveChangesAsync();
        return Ok(order);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpGet]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _db.Orders.Include(o => o.Items).ToListAsync();
        return Ok(orders);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOrder(Guid id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound("Order not found");

        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Order deleted successfully" });
    }
}
