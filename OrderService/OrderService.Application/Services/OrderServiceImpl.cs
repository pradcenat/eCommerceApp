using AutoMapper;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common;
using OrderService.Application.DTO;
using OrderService.Application.Interfaces;
using OrderService.Application.RequestResponse;
using OrderService.Domain.Entity;

namespace OrderService.Application.Services
{
    public class OrderServiceImpl : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMessagePublisher _messagePublisher;
        private readonly IMapper _mapper;
        private readonly ILogger<OrderServiceImpl> _logger;

        public OrderServiceImpl(
            IOrderRepository orderRepository,
            IMessagePublisher messagePublisher,
            IMapper mapper,
            ILogger<OrderServiceImpl> logger)
        {
            _orderRepository = orderRepository;
            _messagePublisher = messagePublisher;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request)
        {
            // Map order
            var order = _mapper.Map<Order>(request);

            // Map order items
            order.OrderItems = request.Items.Select(item =>
            {
                var orderItem = _mapper.Map<OrderItem>(item);
                orderItem.OrderId = order.Id;
                return orderItem;
            }).ToList();

            // Calculate total
            order.TotalAmount = order.OrderItems.Sum(i => i.UnitPrice * i.Quantity);

            await _orderRepository.AddAsync(order);

            _logger.LogInformation(
                "Order created: {OrderId} for user: {UserId}",
                order.Id, order.UserId);

            // Publish OrderPlaced event to Service Bus
            var orderPlacedEvent = new OrderPlacedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                UserEmail = order.UserEmail,
                PlacedAt = order.CreatedAt,
                Items = order.OrderItems.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            await _messagePublisher.PublishOrderPlacedAsync(orderPlacedEvent);

            _logger.LogInformation(
                "OrderPlaced event published for order: {OrderId}", order.Id);

            return _mapper.Map<OrderDto>(order);
        }

        public async Task<OrderDto?> GetByIdAsync(Guid id)
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order is null)
                throw new OrderNotFoundException(id.ToString());

            return _mapper.Map<OrderDto>(order);
        }

        public async Task<IEnumerable<OrderDto>> GetByUserIdAsync(Guid userId)
        {
            var orders = await _orderRepository.GetByUserIdAsync(userId);
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<IEnumerable<OrderDto>> GetAllAsync()
        {
            var orders = await _orderRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<OrderDto>>(orders);
        }

        public async Task<OrderDto?> UpdateStatusAsync(Guid orderId, string status)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order is null)
                throw new OrderNotFoundException(orderId.ToString());

            if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                throw new ArgumentException($"Invalid order status: {status}");

            order.Status = orderStatus;
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation(
                "Order {OrderId} status updated to {Status}",
                orderId, status);

            return _mapper.Map<OrderDto>(order);
        }

        public async Task<bool> CancelOrderAsync(Guid orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order is null)
                throw new OrderNotFoundException(orderId.ToString());

            // Can only cancel if Pending or Confirmed
            if (order.Status != OrderStatus.Pending &&
                order.Status != OrderStatus.Confirmed)
                throw new OrderCannotBeCancelledException(orderId.ToString());

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("Order {OrderId} cancelled", orderId);
            return true;
        }
    }
}
