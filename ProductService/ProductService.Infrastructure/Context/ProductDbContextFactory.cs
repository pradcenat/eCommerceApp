using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ProductService.Infrastructure.Context
{
    public class ProductDbContextFactory
        : IDesignTimeDbContextFactory<ProductDbContext>
    {
        public ProductDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ProductDbContext>();

            optionsBuilder.UseSqlServer(
                "Server=DESKTOP-5L3O3K9;Database=eCommerce_ProductDb;Trusted_Connection=True;TrustServerCertificate=True;");

            return new ProductDbContext(optionsBuilder.Options);
        }
    }
}