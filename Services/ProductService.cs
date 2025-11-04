using ManokshaApi.Data;
using ManokshaApi.Models;
using ManokshaApi.DTO;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManokshaApi.Services
{
    public class ProductService : IProductService
    {
        private readonly AppDbContext _context;

        public ProductService(AppDbContext context)
        {
            _context = context;
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
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<Product?> UpdateProductAsync(Guid id, Product product)
        {
            var existing = await _context.Products.FindAsync(id);
            if (existing == null) return null;

            existing.Name = product.Name;
            existing.Category = product.Category;
            existing.Price = product.Price;
            existing.ImageUrl = product.ImageUrl;
            existing.IsActive = product.IsActive;
            existing.Stock = product.Stock;

            await _context.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            var existing = await _context.Products.FindAsync(id);
            if (existing == null) return false;

            _context.Products.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        // --------------------
        // Filtering / Searching / Sorting / Pagination
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

            // Search by name or category
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.Name.ToLower().Contains(s)
                                         || p.Category.ToLower().Contains(s));
            }

            // Filter by category
            if (!string.IsNullOrWhiteSpace(category))
            {
                var c = category.Trim().ToLower();
                query = query.Where(p => p.Category.ToLower() == c);
            }

            // Filter by price
            if (minPrice.HasValue && minPrice.Value > 0)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue && maxPrice.Value > 0)
                query = query.Where(p => p.Price <= maxPrice.Value);

            // Sorting
            query = sort?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name_desc" => query.OrderByDescending(p => p.Name),
                _ => query.OrderBy(p => p.Name)
            };

            var total = await query.LongCountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedResult<Product>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = total,
                Items = items
            };
        }

        // --------------------
        // Activate / Deactivate product
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
