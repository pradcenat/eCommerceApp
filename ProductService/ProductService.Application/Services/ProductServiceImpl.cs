using AutoMapper;
using ProductService.Application.Common;
using ProductService.Application.DTO;
using ProductService.Application.Interfaces;
using ProductService.Application.RequestResponse;
using ProductService.Domain.Entity;

namespace ProductService.Application.Services
{
    public class ProductServiceImpl : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICacheService _cacheService;
        private readonly IMapper _mapper;

        private const string AllProductsCacheKey = "products:all";
        private const string ProductCachePrefix = "product:";
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(30);

        public ProductServiceImpl(
            IProductRepository productRepository,
            ICacheService cacheService,
            IMapper mapper)
        {
            _productRepository = productRepository;
            _cacheService = cacheService;
            _mapper = mapper;
        }

        public async Task<ProductDto> CreateAsync(ProductRequest request)
        {
            var product = _mapper.Map<Product>(request);
            await _productRepository.AddAsync(product);
            await _cacheService.RemoveAsync(AllProductsCacheKey);
            return _mapper.Map<ProductDto>(product);
        }

        public async Task<ProductDto?> GetByIdAsync(Guid id)
        {
            var cacheKey = $"{ProductCachePrefix}{id}";
            var cached = await _cacheService.GetAsync<ProductDto>(cacheKey);
            if (cached is not null)
                return cached;

            var product = await _productRepository.GetByIdAsync(id);
            if (product is null)
                throw new ProductNotFoundException(id.ToString());

            var productDto = _mapper.Map<ProductDto>(product);
            await _cacheService.SetAsync(cacheKey, productDto, CacheExpiry);
            return productDto;
        }

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var cached = await _cacheService
                .GetAsync<IEnumerable<ProductDto>>(AllProductsCacheKey);
            if (cached is not null)
                return cached;

            var products = await _productRepository.GetAllAsync();
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
            await _cacheService.SetAsync(AllProductsCacheKey, productDtos, CacheExpiry);
            return productDtos;
        }

        public async Task<IEnumerable<ProductDto>> GetByCategoryAsync(string category)
        {
            var cacheKey = $"products:category:{category.ToLower()}";
            var cached = await _cacheService.GetAsync<IEnumerable<ProductDto>>(cacheKey);
            if (cached is not null)
                return cached;

            var products = await _productRepository.GetByCategoryAsync(category);
            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(products);
            await _cacheService.SetAsync(cacheKey, productDtos, CacheExpiry);
            return productDtos;
        }

        public async Task<ProductDto?> UpdateAsync(UpdateProductRequest request)
        {
            var product = await _productRepository.GetByIdAsync(request.Id);
            if (product is null)
                throw new ProductNotFoundException(request.Id.ToString());

            _mapper.Map(request, product);
            await _productRepository.UpdateAsync(product);

            await _cacheService.RemoveAsync($"{ProductCachePrefix}{request.Id}");
            await _cacheService.RemoveAsync(AllProductsCacheKey);

            return _mapper.Map<ProductDto>(product);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product is null)
                throw new ProductNotFoundException(id.ToString());

            await _productRepository.DeleteAsync(product);

            await _cacheService.RemoveAsync($"{ProductCachePrefix}{id}");
            await _cacheService.RemoveAsync(AllProductsCacheKey);

            return true;
        }

        public async Task UpdateStockAsync(Guid productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product is null)
                throw new ProductNotFoundException(productId.ToString());

            if (product.StockQuantity < quantity)
                throw new InsufficientStockException(
                    productId.ToString(), product.StockQuantity, quantity);

            product.StockQuantity -= quantity;
            product.UpdatedAt = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product);

            await _cacheService.RemoveAsync($"{ProductCachePrefix}{productId}");
            await _cacheService.RemoveAsync(AllProductsCacheKey);
        }
    }
}