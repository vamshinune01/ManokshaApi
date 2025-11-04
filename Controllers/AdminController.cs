using ManokshaApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManokshaApi.Controllers
{
    [ApiController, Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        public AdminController(AppDbContext db) { _db = db; }

        [HttpGet("stats")]
        public async Task<IActionResult> Stats()
        {
            var totalOrders = await _db.Orders.CountAsync();
            var dailyOrders = await _db.Orders.Where(o => o.CreatedAt >= DateTime.UtcNow.AddDays(-1)).CountAsync();
            var topProducts = await _db.Orders
                .SelectMany(o => o.Items)
                .GroupBy(i => i.ProductName)
                .Select(g => new { Product = g.Key, Count = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            return Ok(new { totalOrders, dailyOrders, topProducts });
        }
    }
}
