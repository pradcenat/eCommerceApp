namespace OrderService.Application.Common
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

    public class OrderNotFoundException : Exception
    {
        public OrderNotFoundException(string identifier)
            : base($"Order '{identifier}' was not found.") { }
    }

    public class OrderCannotBeCancelledException : Exception
    {
        public OrderCannotBeCancelledException(string orderId)
            : base($"Order '{orderId}' cannot be cancelled in its current status.") { }
    }

    public class ProductServiceUnavailableException : Exception
    {
        public ProductServiceUnavailableException()
            : base("Product service is currently unavailable. Please try again later.") { }
    }
}
