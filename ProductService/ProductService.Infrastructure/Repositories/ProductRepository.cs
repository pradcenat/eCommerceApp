using Microsoft.EntityFrameworkCore;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entity;
using ProductService.Infrastructure.Context;

namespace ProductService.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductDbContext _context;

        public ProductRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task<Product?> GetByIdAsync(Guid id)
            => await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        public async Task<IEnumerable<Product>> GetAllAsync()
            => await _context.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

        public async Task<IEnumerable<Product>> GetByCategoryAsync(string category)
            => await _context.Products
                .AsNoTracking()
                .Where(p => p.Category.ToLower() == category.ToLower() && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Product product)
        {
            product.IsActive = false;
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(Guid id)
            => await _context.Products.AnyAsync(p => p.Id == id && p.IsActive);
    }
}
