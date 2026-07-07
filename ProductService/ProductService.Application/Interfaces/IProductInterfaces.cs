using ProductService.Application.DTO;
using ProductService.Application.RequestResponse;
using ProductService.Domain.Entity;

namespace ProductService.Application.Interfaces
{
    public interface IProductRepository
    {
        Task<Product?> GetByIdAsync(Guid id);
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> GetByCategoryAsync(string category);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(Product product);
        Task<bool> ExistsAsync(Guid id);
    }

    public interface IProductService
    {
        Task<ProductDto> CreateAsync(ProductRequest request);
        Task<ProductDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<ProductDto>> GetAllAsync();
        Task<IEnumerable<ProductDto>> GetByCategoryAsync(string category);
        Task<ProductDto?> UpdateAsync(UpdateProductRequest request);
        Task<bool> DeleteAsync(Guid id);
        Task UpdateStockAsync(Guid productId, int quantity);
    }

    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
    }
}
