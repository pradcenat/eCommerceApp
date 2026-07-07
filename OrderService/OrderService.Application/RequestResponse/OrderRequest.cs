namespace OrderService.Application.RequestResponse
{
    public class CreateOrderItemRequest
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateOrderRequest
    {
        public Guid UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public List<CreateOrderItemRequest> Items { get; set; } = new();
    }

    public class UpdateOrderStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
