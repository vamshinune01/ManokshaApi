using ManokshaApi.Data;
using ManokshaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManokshaApi.Controllers
{
    [ApiController, Route("api/products")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ProductController(AppDbContext db) { _db = db; }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _db.Products.Where(p => p.IsActive).ToListAsync());

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p == null) return NotFound();
            return Ok(p);
        }

        [HttpPost] // Admin create
        public async Task<IActionResult> Create([FromBody] Product p)
        {
            _db.Products.Add(p);
            await _db.SaveChangesAsync();
            return Ok(p);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Product incoming)
        {
            var p = await _db.Products.FindAsync(id);
            if (p == null) return NotFound();
            p.Name = incoming.Name;
            p.Price = incoming.Price;
            p.Category = incoming.Category;
            p.ImageUrl = incoming.ImageUrl;
            p.IsActive = incoming.IsActive;
            p.Stock = incoming.Stock;
            await _db.SaveChangesAsync();
            return Ok(p);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p == null) return NotFound();
            p.IsActive = false;
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
