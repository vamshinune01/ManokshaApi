using ManokshaApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ManokshaApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<ReturnRequest> ReturnRequests { get; set; } = null!;
    }
}
