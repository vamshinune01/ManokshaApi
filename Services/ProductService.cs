using ManokshaApi.Data;
using ManokshaApi.Models;
using ManokshaApi.DTO;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace ManokshaApi.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;
        private readonly FirebaseStorageService _firebaseStorageService;

        public ProductService(AppDbContext context)
        {
            _context = context;
            _firebaseStorageService = new FirebaseStorageService(); // ✅ Firebase initialized here
        }

        // --------------------
        // Basic CRUD
        // --------------------
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(Guid id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task<Product> CreateProductAsync(Product product)
        {
            // ✅ If product has image as local path, upload to Firebase
            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                if (File.Exists(product.ImageUrl))
                {
                    using var stream = File.OpenRead(product.ImageUrl);
                    string imageUrl = await _firebaseStorageService.UploadFileAsync(stream, Path.GetFileName(product.ImageUrl), "image/jpeg");
                    product.ImageUrl = imageUrl;
                }
                else if (product.ImageUrl.StartsWith("data:image"))
                {
                    // ✅ Handle base64 uploads (from web/mobile)
                    string base64Data = product.ImageUrl.Split(',')[1];
                    byte[] bytes = Convert.FromBase64String(base64Data);
                    using var stream = new MemoryStream(bytes);
                    string imageUrl = await _firebaseStorageService.UploadFileAsync(stream, $"{Guid.NewGuid()}.jpg", "image/jpeg");
                    product.ImageUrl = imageUrl;
                }
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product?> UpdateProductAsync(Guid id, Product updatedProduct)
        {
            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
                return null;

            // ✅ Handle new image upload
            if (!string.IsNullOrEmpty(updatedProduct.ImageUrl))
            {
                if (File.Exists(updatedProduct.ImageUrl))
                {
                    using var stream = File.OpenRead(updatedProduct.ImageUrl);
                    string newImageUrl = await _firebaseStorageService.UploadFileAsync(stream, Path.GetFileName(updatedProduct.ImageUrl), "image/jpeg");
                    existingProduct.ImageUrl = newImageUrl;
                }
                else if (updatedProduct.ImageUrl.StartsWith("data:image"))
                {
                    // ✅ Base64 (for web apps)
                    string base64Data = updatedProduct.ImageUrl.Split(',')[1];
                    byte[] bytes = Convert.FromBase64String(base64Data);
                    using var stream = new MemoryStream(bytes);
                    string imageUrl = await _firebaseStorageService.UploadFileAsync(stream, $"{Guid.NewGuid()}.jpg", "image/jpeg");
                    existingProduct.ImageUrl = imageUrl;
                }
            }

            existingProduct.Name = updatedProduct.Name;
            existingProduct.Description = updatedProduct.Description;
            existingProduct.Price = updatedProduct.Price;
            existingProduct.Category = updatedProduct.Category;
            existingProduct.IsActive = updatedProduct.IsActive;

            await _context.SaveChangesAsync();
            return existingProduct;
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return false;

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        // --------------------
        // Filtering / Search / Sort / Pagination
        // --------------------
        public async Task<PaginatedResult<Product>> GetProductsAsync(
            string? search,
            string? category,
            decimal? minPrice,
            decimal? maxPrice,
            string? sort,
            int page,
            int pageSize)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_asc" => query.OrderBy(p => p.Name),
                "name_desc" => query.OrderByDescending(p => p.Name),
                _ => query.OrderBy(p => p.Name)
            };

            var totalCount = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedResult<Product>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items
            };
        }

        // --------------------
        // Activate / Deactivate Product
        // --------------------
        public async Task<bool> SetProductActiveStateAsync(Guid id, bool isActive)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return false;

            product.IsActive = isActive;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
