using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entity;

namespace ProductService.Infrastructure.Context
{
    public class ProductDbContext : DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
                entity.Property(p => p.Description).IsRequired().HasMaxLength(2000);
                entity.Property(p => p.Price).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(p => p.Category).IsRequired().HasMaxLength(100);
                entity.HasIndex(p => p.Category);
                entity.Property(p => p.IsActive).HasDefaultValue(true);
                entity.Property(p => p.CreatedAt).IsRequired();
                entity.Property(p => p.UpdatedAt).IsRequired();
            });

            // Seed data
            modelBuilder.Entity<Product>().HasData(
                new Product
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Blue Denim Jeans",
                    Description = "Classic blue denim jeans for everyday wear",
                    Price = 59.99m,
                    StockQuantity = 100,
                    Category = "Clothing",
                    ImageUrl = "https://example.com/jeans.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "White T-Shirt",
                    Description = "Premium cotton white t-shirt",
                    Price = 24.99m,
                    StockQuantity = 200,
                    Category = "Clothing",
                    ImageUrl = "https://example.com/tshirt.jpg",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
