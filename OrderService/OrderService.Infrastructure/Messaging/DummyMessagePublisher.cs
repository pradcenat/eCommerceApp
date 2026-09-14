using OrderService.Application.DTO;
using OrderService.Application.Interfaces;

namespace OrderService.Infrastructure.Messaging
{
    public class DummyMessagePublisher : IMessagePublisher
    {
        public Task PublishOrderPlacedAsync(OrderPlacedEvent orderEvent)
        {
            // Service Bus not configured yet
            // Order saved in DB successfully
            return Task.CompletedTask;
        }
    }
}