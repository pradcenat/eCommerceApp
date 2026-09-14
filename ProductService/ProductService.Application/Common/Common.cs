namespace ProductService.Application.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
            => new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> FailureResponse(string message, List<string>? errors = null)
            => new() { Success = false, Message = message, Errors = errors ?? new() };

        public static ApiResponse<T> SuccessResponse(string message)
            => new() { Success = true, Message = message };
    }

    public class ProductNotFoundException : Exception
    {
        public ProductNotFoundException(string identifier)
            : base($"Product '{identifier}' was not found.") { }
    }

    public class InsufficientStockException : Exception
    {
        public InsufficientStockException(string productId, int available, int requested)
            : base($"Insufficient stock for product '{productId}'. Available: {available}, Requested: {requested}.") { }
    }
}
