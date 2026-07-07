using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common;
using OrderService.Application.Interfaces;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Text.Json;

namespace OrderService.Infrastructure.Http
{
    /// <summary>
    /// HTTP Client for calling ProductService.
    /// Implements Polly Circuit Breaker AND Retry pattern.
    /// 
    /// Retry — try again on transient failures
    /// Circuit Breaker — stop trying if too many failures
    /// Together they protect the system from cascade failures
    /// </summary>
    public class ProductServiceClient : IProductServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductServiceClient> _logger;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy;

        public ProductServiceClient(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ProductServiceClient> logger)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(
                configuration["ServiceUrls:ProductService"]!);
            _logger = logger;

            // Retry Policy — retry 3 times with exponential backoff
            _retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => !r.IsSuccessStatusCode &&
                               r.StatusCode != System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, attempt)), // 2s, 4s, 8s
                    onRetry: (outcome, timespan, attempt, context) =>
                    {
                        _logger.LogWarning(
                            "Retry {Attempt} calling ProductService. " +
                            "Waiting {Delay}s. Reason: {Reason}",
                            attempt,
                            timespan.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    });

            // Circuit Breaker Policy
            _circuitBreakerPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => !r.IsSuccessStatusCode)
                .AdvancedCircuitBreakerAsync(
                    failureThreshold: 0.5,
                    samplingDuration: TimeSpan.FromSeconds(30),
                    minimumThroughput: 5,
                    durationOfBreak: TimeSpan.FromSeconds(30),
                    onBreak: (outcome, duration) =>
                    {
                        _logger.LogWarning(
                            "Circuit breaker OPENED for ProductService for {Duration}s",
                            duration.TotalSeconds);
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation(
                            "Circuit breaker CLOSED — ProductService recovered");
                    },
                    onHalfOpen: () =>
                    {
                        _logger.LogInformation(
                            "Circuit breaker HALF-OPEN — Testing ProductService");
                    });
        }

        public async Task<bool> CheckProductAvailabilityAsync(Guid productId, int quantity)
        {
            try
            {
                // Wrap retry inside circuit breaker
                var response = await _circuitBreakerPolicy.ExecuteAsync(async () =>
                    await _retryPolicy.ExecuteAsync(async () =>
                        await _httpClient.GetAsync(
                            $"/api/product/{productId}")));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "ProductService returned {StatusCode} for product {ProductId}",
                        response.StatusCode, productId);
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ProductAvailabilityResponse>(
                    content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result?.Data?.StockQuantity >= quantity;
            }
            catch (BrokenCircuitException)
            {
                _logger.LogError(
                    "Circuit breaker OPEN — ProductService unavailable. " +
                    "Cannot check availability for product: {ProductId}", productId);

                // Fail open — allow order to proceed
                // In production decide: fail open or fail closed based on business rules
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error checking product availability: {ProductId}", productId);
                return true; // Fail open
            }
        }
    }

    public class ProductAvailabilityResponse
    {
        public ProductData? Data { get; set; }
    }

    public class ProductData
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public decimal Price { get; set; }
    }
}
