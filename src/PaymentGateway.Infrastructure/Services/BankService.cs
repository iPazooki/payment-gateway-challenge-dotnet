using System.Net;
using System.Text;
using System.Text.Json;

using Polly;
using Polly.Fallback;
using Polly.Retry;

namespace PaymentGateway.Infrastructure.Services;

internal class BankService(IHttpClientFactory httpClientFactory, ILogger<BankService> logger) : IBankService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("BankApi");

    public async Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            RetryStrategyOptions<HttpResponseMessage> retryOptions = new()
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(3),
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .Handle<TimeoutException>()
                    .HandleResult(response => response.StatusCode == HttpStatusCode.GatewayTimeout),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Bank service call failed. Retry attempt {RetryCount} in {Delay}ms.",
                        args.AttemptNumber, args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            };

            FallbackStrategyOptions<HttpResponseMessage> fallbackOptions = new()
            {
                FallbackAction = _ =>
                    Outcome.FromResultAsValueTask(new HttpResponseMessage(HttpStatusCode.GatewayTimeout))
            };

            ResiliencePipeline<HttpResponseMessage> resiliencePipeline =
                new ResiliencePipelineBuilder<HttpResponseMessage>()
                    .AddRetry(retryOptions)
                    .AddFallback(fallbackOptions)
                    .Build();

            string json = JsonSerializer.Serialize(request,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            StringContent content = new(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await resiliencePipeline.ExecuteAsync(async _ =>
                await _httpClient.PostAsync("payments", content, cancellationToken), cancellationToken);

            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            BankPaymentResponse? bankResponse = JsonSerializer.Deserialize<BankPaymentResponse>(
                responseJson,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

            return bankResponse ?? new BankPaymentResponse { Authorized = false };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing payment with bank");
            return new BankPaymentResponse { Authorized = false };
        }
    }
}