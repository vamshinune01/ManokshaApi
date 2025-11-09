using ManokshaApi.Data;
using ManokshaApi.Models;
using ManokshaApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ManokshaApi.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly FirebaseStorageService _firebaseService;

        public ProductController(AppDbContext db, FirebaseStorageService firebaseService)
        {
            _db = db;
            _firebaseService = firebaseService;
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

        // ✅ Create product (with local image upload)
        [HttpPost]
        [RequestSizeLimit(10_000_000)] // limit 10MB
        public async Task<IActionResult> Create([FromForm] ProductUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Image file is required.");

            // Upload file to Firebase
            string imageUrl;
            using (var stream = request.File.OpenReadStream())
            {
                imageUrl = await _firebaseService.UploadFileAsync(stream, request.File.FileName, request.File.ContentType);
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

    // DTO for multipart/form-data
    public class ProductUploadRequest
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public IFormFile File { get; set; } // uploaded file
    }
}
