using ManokshaApi.Data;
using ManokshaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManokshaApi.Controllers
{
    [ApiController, Route("api/returns")]
    public class ReturnController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ReturnController(AppDbContext db) { _db = db; }

        [HttpPost("request")]
        public async Task<IActionResult> RequestReturn([FromBody] ReturnRequest req)
        {
            var order = await _db.Orders.FindAsync(req.OrderId);
            if (order == null) return BadRequest("Invalid order.");
            _db.ReturnRequests.Add(req);
            order.OrderStatus = "ReturnRequested";
            await _db.SaveChangesAsync();
            return Ok(req);
        }

        [HttpGet("order/{orderId:guid}")]
        public async Task<IActionResult> GetByOrder(Guid orderId)
        {
            var list = await _db.ReturnRequests.Where(r => r.OrderId == orderId).ToListAsync();
            return Ok(list);
        }
    }
}
