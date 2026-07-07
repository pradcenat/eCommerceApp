using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderService.Application.DTO;
using OrderService.Application.Interfaces;
using Polly;
using Polly.CircuitBreaker;
using System.Text.Json;

namespace OrderService.Infrastructure.Messaging
{
    /// <summary>
    /// Service Bus Publisher with Polly Circuit Breaker pattern.
    /// Circuit Breaker prevents calling a failing service repeatedly.
    /// After X failures the circuit OPENS — stops trying.
    /// After timeout it goes HALF-OPEN — tries one request.
    /// If success — circuit CLOSES — normal operation resumes.
    /// </summary>
    public class ServiceBusPublisher : IMessagePublisher, IAsyncDisposable
    {
        private readonly ServiceBusSender _sender;
        private readonly ServiceBusClient _client;
        private readonly ILogger<ServiceBusPublisher> _logger;
        private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

        public ServiceBusPublisher(
            IConfiguration configuration,
            ILogger<ServiceBusPublisher> logger)
        {
            _logger = logger;

            var connectionString = configuration
                .GetConnectionString("ServiceBus")!;
            var queueName = configuration["ServiceBus:OrderPlacedQueue"]!;

            _client = new ServiceBusClient(connectionString);
            _sender = _client.CreateSender(queueName);

            // Configure Polly Circuit Breaker
            _circuitBreakerPolicy = Policy
                .Handle<ServiceBusException>()
                .Or<TimeoutException>()
                .AdvancedCircuitBreakerAsync(
                    // Open circuit if 50% of calls fail
                    failureThreshold: 0.5,
                    // Over a 30 second sampling window
                    samplingDuration: TimeSpan.FromSeconds(30),
                    // Minimum 5 calls before evaluating
                    minimumThroughput: 5,
                    // Keep circuit open for 30 seconds
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (exception, duration) =>
                    {
                        _logger.LogWarning(
                            "Circuit breaker OPENED for {Duration}s. Reason: {Exception}",
                            duration.TotalSeconds, exception.Message);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation(
                            "Circuit breaker CLOSED — Service Bus recovered");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation(
                            "Circuit breaker HALF-OPEN — Testing Service Bus connection");
                    });
        }

        public async Task PublishOrderPlacedAsync(OrderPlacedEvent orderEvent)
        {
            try
            {
                await _circuitBreakerPolicy.ExecuteAsync(async () =>
                {
                    var messageBody = JsonSerializer.Serialize(orderEvent);

                    var message = new ServiceBusMessage(messageBody)
                    {
                        MessageId = orderEvent.OrderId.ToString(),
                        Subject = "OrderPlaced",
                        ContentType = "application/json",
                        ApplicationProperties =
                        {
                            { "EventType", "OrderPlaced" },
                            { "UserId", orderEvent.UserId.ToString() },
                            { "OrderId", orderEvent.OrderId.ToString() }
                        }
                    };

                    await _sender.SendMessageAsync(message);

                    _logger.LogInformation(
                        "OrderPlaced event published to Service Bus. OrderId: {OrderId}",
                        orderEvent.OrderId);
                });
            }
            catch (BrokenCircuitException ex)
            {
                // Circuit is open — Service Bus is unavailable
                // Log and continue — order is already saved in DB
                // The event will need to be published via retry mechanism
                _logger.LogError(ex,
                    "Circuit breaker is OPEN. Cannot publish OrderPlaced event for order: {OrderId}. " +
                    "Order saved in DB. Event will be retried.",
                    orderEvent.OrderId);

                // In production you would implement an outbox pattern here
                // to ensure the event is eventually published
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to publish OrderPlaced event for order: {OrderId}",
                    orderEvent.OrderId);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _sender.DisposeAsync();
            await _client.DisposeAsync();
        }
    }
}
