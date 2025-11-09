using ManokshaApi.Data;
using ManokshaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManokshaApi.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProductController(AppDbContext db)
        {
            _db = db;
        }

        // ✅ Get all active products
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _db.Products.Where(p => p.IsActive).ToListAsync();
            return Ok(products);
        }

        // ✅ Get product by ID
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        // ✅ Create product (admin)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            product.Id = Guid.NewGuid();
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = product.Id }, product);
        }

        // ✅ Update product
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Product incoming)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Name = incoming.Name;
            product.Price = incoming.Price;
            product.Category = incoming.Category;
            product.ImageUrl = incoming.ImageUrl;
            product.IsActive = incoming.IsActive;
            product.Stock = incoming.Stock;

            await _db.SaveChangesAsync();
            return Ok(product);
        }

        // ✅ Soft delete product
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsActive = false;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Product deactivated successfully." });
        }

        // ✅ Toggle active state
        [HttpPatch("{id:guid}/active")]
        public async Task<IActionResult> SetActiveState(Guid id, [FromQuery] bool isActive)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsActive = isActive;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
