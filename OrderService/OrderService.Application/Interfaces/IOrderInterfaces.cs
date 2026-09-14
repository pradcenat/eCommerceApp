using OrderService.Application.DTO;
using OrderService.Application.RequestResponse;
using OrderService.Domain.Entity;

namespace OrderService.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task<Order?> GetByIdAsync(Guid id);
        Task<IEnumerable<Order>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<Order>> GetAllAsync();
        Task AddAsync(Order order);
        Task UpdateAsync(Order order);
    }

    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(CreateOrderRequest request);
        Task<OrderDto?> GetByIdAsync(Guid id);
        Task<IEnumerable<OrderDto>> GetByUserIdAsync(Guid userId);
        Task<IEnumerable<OrderDto>> GetAllAsync();
        Task<OrderDto?> UpdateStatusAsync(Guid orderId, string status);
        Task<bool> CancelOrderAsync(Guid orderId);
    }

    public interface IMessagePublisher
    {
        Task PublishOrderPlacedAsync(OrderPlacedEvent orderEvent);
    }

    public interface IProductServiceClient
    {
        Task<bool> CheckProductAvailabilityAsync(Guid productId, int quantity);
    }
}
