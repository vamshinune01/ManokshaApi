using ManokshaApi.Data;
using ManokshaApi.DTO;
using ManokshaApi.Models;
using ManokshaApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManokshaApi.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly FirebaseStorageService _firebase;

        public ProductController(AppDbContext db, FirebaseStorageService firebase)
        {
            _db = db;
            _firebase = firebase;
        }

        // ---------------------------
        // Get all active products
        // ---------------------------
        [Authorize(Roles = "Customer,Worker,SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _db.Products.Where(p => p.IsActive).ToListAsync();

            return Ok(products.Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Category = p.Category,
                Price = p.Price,
                Stock = p.Stock,
                IsActive = p.IsActive,
                ImageUrl = p.ImageUrl
            }));
        }

        // ---------------------------
        // Get product by Id
        // ---------------------------
        [Authorize(Roles = "Customer,Worker,SuperAdmin")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p == null) return NotFound();

            return Ok(new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Category = p.Category,
                Price = p.Price,
                Stock = p.Stock,
                IsActive = p.IsActive,
                ImageUrl = p.ImageUrl
            });
        }

        // ---------------------------
        // Create product (Worker/SuperAdmin)
        // ---------------------------
        [Authorize(Roles = "Worker,SuperAdmin")]
        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> Create([FromForm] ProductUploadRequest request)
        {
            string imageUrl;
            using (var stream = request.File.OpenReadStream())
            {
                imageUrl = await _firebase.UploadFileAsync(stream, request.File.FileName, request.File.ContentType);
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Category = request.Category,
                Price = request.Price,
                Stock = request.Stock,
                IsActive = request.IsActive,
                ImageUrl = imageUrl
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = product.Id }, new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Category = product.Category,
                Price = product.Price,
                Stock = product.Stock,
                IsActive = product.IsActive,
                ImageUrl = product.ImageUrl
            });
        }

        // ---------------------------
        // Update product (Worker/SuperAdmin)
        // ---------------------------
        [Authorize(Roles = "Worker,SuperAdmin")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ProductRequest request)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Name = request.Name;
            product.Category = request.Category;
            product.Price = request.Price;
            product.Stock = request.Stock;
            product.IsActive = request.IsActive;

            await _db.SaveChangesAsync();

            return Ok(new ProductResponse
            {
                Id = product.Id,
                Name = product.Name,
                Category = product.Category,
                Price = product.Price,
                Stock = product.Stock,
                IsActive = product.IsActive,
                ImageUrl = product.ImageUrl
            });
        }

        // ---------------------------
        // Deactivate product (SuperAdmin)
        // ---------------------------
        [Authorize(Roles = "SuperAdmin")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsActive = false;
            await _db.SaveChangesAsync();
            return Ok(new { message = "Product deactivated" });
        }

        // ---------------------------
        // Toggle active state (SuperAdmin)
        // ---------------------------
        [Authorize(Roles = "SuperAdmin")]
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
