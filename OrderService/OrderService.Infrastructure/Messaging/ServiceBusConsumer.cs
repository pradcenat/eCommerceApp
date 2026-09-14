using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Domain.Entity;
using System.Text.Json;

namespace OrderService.Infrastructure.Messaging
{
    /// <summary>
    /// Background service that listens for messages from other services.
    /// Runs continuously as a hosted service.
    /// Currently listens for order status update events.
    /// </summary>
    public class ServiceBusConsumer : BackgroundService, IAsyncDisposable
    {
        private readonly ServiceBusProcessor _processor;
        private readonly ServiceBusClient _client;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ServiceBusConsumer> _logger;

        public ServiceBusConsumer(
            IConfiguration configuration,
            IServiceProvider serviceProvider,
            ILogger<ServiceBusConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            var connectionString = configuration
                .GetConnectionString("ServiceBus")!;
            var queueName = configuration["ServiceBus:OrderStatusQueue"]!;

            _client = new ServiceBusClient(connectionString);

            _processor = _client.CreateProcessor(
                queueName,
                new ServiceBusProcessorOptions
                {
                    MaxConcurrentCalls = 5,
                    AutoCompleteMessages = false,
                    MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(5)
                });

            _processor.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ProcessErrorAsync;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Service Bus Consumer started");
            await _processor.StartProcessingAsync(stoppingToken);

            // Keep running until cancelled
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            var messageBody = args.Message.Body.ToString();
            var subject = args.Message.Subject;

            _logger.LogInformation(
                "Received message: {Subject} — MessageId: {MessageId}",
                subject, args.Message.MessageId);

            try
            {
                switch (subject)
                {
                    case "UpdateOrderStatus":
                        await HandleOrderStatusUpdateAsync(messageBody);
                        break;
                    default:
                        _logger.LogWarning("Unknown message subject: {Subject}", subject);
                        break;
                }

                // Complete message — remove from queue
                await args.CompleteMessageAsync(args.Message);

                _logger.LogInformation(
                    "Message processed successfully: {MessageId}",
                    args.Message.MessageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing message: {MessageId}", args.Message.MessageId);

                // Abandon — returns to queue for retry
                await args.AbandonMessageAsync(args.Message);
            }
        }

        private async Task HandleOrderStatusUpdateAsync(string messageBody)
        {
            var statusUpdate = JsonSerializer.Deserialize<OrderStatusUpdateMessage>(messageBody);
            if (statusUpdate is null) return;

            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            await orderService.UpdateStatusAsync(
                statusUpdate.OrderId,
                statusUpdate.NewStatus);

            _logger.LogInformation(
                "Order {OrderId} status updated to {Status} via Service Bus",
                statusUpdate.OrderId, statusUpdate.NewStatus);
        }

        private Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception,
                "Service Bus processor error on {EntityPath}",
                args.EntityPath);
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }

        public new async ValueTask DisposeAsync()
        {
            await _processor.DisposeAsync();
            await _client.DisposeAsync();
        }
    }

    public class OrderStatusUpdateMessage
    {
        public Guid OrderId { get; set; }
        public string NewStatus { get; set; } = string.Empty;
    }
}
