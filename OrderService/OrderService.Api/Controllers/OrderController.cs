using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Common;
using OrderService.Application.Interfaces;
using OrderService.Application.RequestResponse;
using System.Security.Claims;

namespace OrderService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    //[AllowAnonymous]
    [Produces("application/json")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IValidator<CreateOrderRequest> _createValidator;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderService orderService,
            IValidator<CreateOrderRequest> createValidator,
            ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _createValidator = createValidator;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
        {
            var validation = await _createValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResponse("Validation failed", errors));
            }

            _logger.LogInformation(
                "Creating order for user: {UserId}", request.UserId);

            var result = await _orderService.CreateOrderAsync(request);

            return CreatedAtAction(nameof(GetById),
                new { id = result.Id },
                ApiResponse<object>.SuccessResponse(result, "Order created successfully"));
        }

        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("Getting order: {OrderId}", id);
            var result = await _orderService.GetByIdAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(result!));
        }

        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByUserId(Guid userId)
        {
            _logger.LogInformation("Getting orders for user: {UserId}", userId);
            var result = await _orderService.GetByUserIdAsync(userId);
            return Ok(ApiResponse<object>.SuccessResponse(result));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Getting all orders");
            var result = await _orderService.GetAllAsync();
            return Ok(ApiResponse<object>.SuccessResponse(result));
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateStatus(
            Guid id,
            [FromBody] UpdateOrderStatusRequest request)
        {
            _logger.LogInformation(
                "Updating status for order: {OrderId} to {Status}",
                id, request.Status);

            var result = await _orderService.UpdateStatusAsync(id, request.Status);
            return Ok(ApiResponse<object>.SuccessResponse(result!, "Order status updated"));
        }

        [HttpPost("{id:guid}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Cancel(Guid id)
        {
            _logger.LogInformation("Cancelling order: {OrderId}", id);
            await _orderService.CancelOrderAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse("Order cancelled successfully"));
        }
        [HttpGet("test-auth")]
        [Authorize]
        public IActionResult TestAuth()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            return Ok(new { userId, email, message = "JWT working" });
        }
    }
}
