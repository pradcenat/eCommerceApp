using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Common;
using ProductService.Application.Interfaces;
using ProductService.Application.RequestResponse;

namespace ProductService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IValidator<ProductRequest> _createValidator;
        private readonly IValidator<UpdateProductRequest> _updateValidator;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IProductService productService,
            IValidator<ProductRequest> createValidator,
            IValidator<UpdateProductRequest> updateValidator,
            ILogger<ProductController> logger)
        {
            _productService = productService;
            _createValidator = createValidator;
            _updateValidator = updateValidator;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Getting all products");
            var result = await _productService.GetAllAsync();
            return Ok(ApiResponse<object>.SuccessResponse(result));
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("Getting product: {ProductId}", id);
            var result = await _productService.GetByIdAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(result!));
        }

        [HttpGet("category/{category}")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByCategory(string category)
        {
            _logger.LogInformation("Getting products by category: {Category}", category);
            var result = await _productService.GetByCategoryAsync(category);
            return Ok(ApiResponse<object>.SuccessResponse(result));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] ProductRequest request)
        {
            var validation = await _createValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResponse("Validation failed", errors));
            }

            _logger.LogInformation("Creating product: {Name}", request.Name);
            var result = await _productService.CreateAsync(request);

            return CreatedAtAction(nameof(GetById),
                new { id = result.Id },
                ApiResponse<object>.SuccessResponse(result, "Product created successfully"));
        }

        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request)
        {
            request.Id = id;
            var validation = await _updateValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResponse("Validation failed", errors));
            }

            _logger.LogInformation("Updating product: {ProductId}", id);
            var result = await _productService.UpdateAsync(request);
            return Ok(ApiResponse<object>.SuccessResponse(result!, "Product updated successfully"));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("Deleting product: {ProductId}", id);
            await _productService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse("Product deleted successfully"));
        }

        [HttpPatch("{id:guid}/stock")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStock(Guid id, [FromBody] UpdateStockRequest request)
        {
            request.ProductId = id;
            _logger.LogInformation("Updating stock for product: {ProductId}", id);
            await _productService.UpdateStockAsync(id, request.Quantity);
            return Ok(ApiResponse<object>.SuccessResponse("Stock updated successfully"));
        }
    }
}
