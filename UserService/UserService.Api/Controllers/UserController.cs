using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Common;
using UserService.Application.Interfaces;
using UserService.Application.RequestResponse;

namespace UserService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IValidator<RegisterRequest> _registerValidator;
        private readonly IValidator<UpdateUserRequest> _updateValidator;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            IValidator<RegisterRequest> registerValidator,
            IValidator<UpdateUserRequest> updateValidator,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _registerValidator = registerValidator;
            _updateValidator = updateValidator;
            _logger = logger;
        }

        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var validation = await _registerValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResponse("Validation failed", errors));
            }

            _logger.LogInformation("Registering user: {Email}", request.Email);
            var result = await _userService.RegisterAsync(request);

            return CreatedAtAction(nameof(GetById),
                new { id = result.Id },
                ApiResponse<object>.SuccessResponse(result, "User registered successfully"));
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            _logger.LogInformation("Getting user: {Id}", id);
            var result = await _userService.GetByIdAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(result!));
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            request.Id = id;
            var validation = await _updateValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResponse("Validation failed", errors));
            }

            _logger.LogInformation("Updating user: {Id}", id);
            var result = await _userService.UpdateAsync(request);
            return Ok(ApiResponse<object>.SuccessResponse(result!, "User updated successfully"));
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            _logger.LogInformation("Deleting user: {Id}", id);
            await _userService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse("User deleted successfully"));
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IValidator<LoginRequest> _loginValidator;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserService userService,
            IValidator<LoginRequest> loginValidator,
            ILogger<AuthController> logger)
        {
            _userService = userService;
            _loginValidator = loginValidator;
            _logger = logger;
        }

        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var validation = await _loginValidator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<object>.FailureResponse("Validation failed", errors));
            }

            _logger.LogInformation("Login attempt: {Email}", request.Email);
            var result = await _userService.LoginAsync(request);
            return Ok(ApiResponse<object>.SuccessResponse(result, "Login successful"));
        }
    }
}
