using ManokshaApi.Models;
using ManokshaApi.DTO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManokshaApi.Services
{
    public interface IProductService
    {
        // 🟢 Basic CRUD
        Task<List<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(Guid id);
        Task<Product> CreateProductAsync(Product product);
        Task<Product?> UpdateProductAsync(Guid id, Product product);
        Task<bool> DeleteProductAsync(Guid id);

        // 🟡 Filtering / Search / Sort / Pagination
        Task<PaginatedResult<Product>> GetProductsAsync(
            string? search,
            string? category,
            decimal? minPrice,
            decimal? maxPrice,
            string? sort,
            int page,
            int pageSize);

        // 🔵 Activate / Deactivate Product
        Task<bool> SetProductActiveStateAsync(Guid id, bool isActive);
    }
}
