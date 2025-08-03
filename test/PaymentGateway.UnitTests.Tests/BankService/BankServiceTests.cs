using WireMock.Server;
using WireMock.Settings;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace PaymentGateway.UnitTests.Tests.BankService;

public class BankServiceTests : IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FakeLogger<Infrastructure.Services.BankService> _logger;
    private readonly WireMockServer _wireMockServer;
    private readonly HttpClient _httpClient;
    private readonly Infrastructure.Services.BankService _bankService;

    public BankServiceTests()
    {
        _wireMockServer = WireMockServer.Start(new WireMockServerSettings
        {
            Port = 0, // Use random available port
            StartAdminInterface = false
        });

        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _logger = new FakeLogger<Infrastructure.Services.BankService>();
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_wireMockServer.Url!)
        };
        
        _httpClientFactory.CreateClient("BankApi").Returns(_httpClient);
        _bankService = new Infrastructure.Services.BankService(_httpClientFactory, _logger);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithValidRequest_ReturnsAuthorizedResponse()
    {
        // Arrange
        var request = new BankPaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryDate = "12/25",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        var expectedResponse = new BankPaymentResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH123"
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse, 
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        _wireMockServer
            .Given(Request.Create()
                .WithPath("/payments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

        // Act
        var result = await _bankService.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.Authorized);
        Assert.Equal("AUTH123", result.AuthorizationCode);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithUnauthorizedResponse_ReturnsUnauthorizedResponse()
    {
        // Arrange
        var request = new BankPaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryDate = "12/25",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        var expectedResponse = new BankPaymentResponse
        {
            Authorized = false,
            AuthorizationCode = null
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse, 
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        _wireMockServer
            .Given(Request.Create()
                .WithPath("/payments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

        // Act
        var result = await _bankService.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.Authorized);
        Assert.Null(result.AuthorizationCode);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithGatewayTimeout_RetriesAndUsesFallback()
    {
        // Arrange
        var request = new BankPaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryDate = "12/25",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        _wireMockServer
            .Given(Request.Create()
                .WithPath("/payments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.GatewayTimeout));

        // Act
        var result = await _bankService.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.Authorized);
        Assert.Null(result.AuthorizationCode);
        
        // Verify retry attempts were logged
        var retryLogs = _logger.LogEntries.Where(e => 
            e.LogLevel == LogLevel.Warning && 
            e.Message.Contains("Bank service call failed")).ToList();
        Assert.True(retryLogs.Count >= 2); // Should have at least 2 retry attempts
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithServerError_RetriesAndReturnsUnauthorized()
    {
        // Arrange
        var request = new BankPaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryDate = "12/25",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        _wireMockServer
            .Given(Request.Create()
                .WithPath("/payments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.InternalServerError));

        // Act
        var result = await _bankService.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.Authorized);
        Assert.Null(result.AuthorizationCode);
        
        // Should log error
        Assert.Contains(_logger.LogEntries, entry => 
            entry.LogLevel == LogLevel.Error && 
            entry.Message.Contains("Error processing payment with bank"));
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithDelayedResponse_SucceedsAfterRetry()
    {
        // Arrange
        var request = new BankPaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryDate = "12/25",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        var expectedResponse = new BankPaymentResponse
        {
            Authorized = true,
            AuthorizationCode = "AUTH456"
        };

        var responseJson = JsonSerializer.Serialize(expectedResponse, 
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        // First call returns timeout, second call succeeds
        _wireMockServer
            .Given(Request.Create()
                .WithPath("/payments")
                .UsingPost())
            .InScenario("RetryScenario")
            .WillSetStateTo("FirstCall")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.GatewayTimeout));

        _wireMockServer
            .Given(Request.Create()
                .WithPath("/payments")
                .UsingPost())
            .InScenario("RetryScenario")
            .WhenStateIs("FirstCall")
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

        // Act
        var result = await _bankService.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.Authorized);
        Assert.Equal("AUTH456", result.AuthorizationCode);
        
        // Should log retry attempt
        Assert.Contains(_logger.LogEntries, entry => 
            entry.LogLevel == LogLevel.Warning && 
            entry.Message.Contains("Bank service call failed"));
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithNullResponseContent_ReturnsUnauthorizedResponse()
    {
        // Arrange
        var request = new BankPaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryDate = "12/25",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        _wireMockServer
            .Given(Request.Create()
                .WithPath("/payments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("null"));

        // Act
        var result = await _bankService.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.Authorized);
        Assert.Null(result.AuthorizationCode);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithInvalidJson_LogsErrorAndReturnsUnauthorized()
    {
        // Arrange
        var request = new BankPaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryDate = "12/25",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        _wireMockServer
            .Given(Request.Create()
                .WithPath("/payments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("invalid json"));

        // Act
        var result = await _bankService.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.Authorized);
        Assert.Null(result.AuthorizationCode);
        Assert.Contains(_logger.LogEntries, entry => 
            entry.LogLevel == LogLevel.Error && 
            entry.Message.Contains("Error processing payment with bank"));
    }

    [Fact]
    public async Task ProcessPaymentAsync_SendsCorrectRequestBody()
    {
        // Arrange
        var request = new BankPaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryDate = "12/25",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        var expectedResponse = new BankPaymentResponse { Authorized = true };
        var responseJson = JsonSerializer.Serialize(expectedResponse, 
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });

        _wireMockServer
            .Given(Request.Create()
                .WithPath("/payments")
                .UsingPost()
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithBody(body => body.Contains("\"card_number\":\"4111111111111111\"") &&
                                body.Contains("\"expiry_date\":\"12/25\"") &&
                                body.Contains("\"currency\":\"USD\"") &&
                                body.Contains("\"amount\":100") &&
                                body.Contains("\"cvv\":\"123\"")))
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseJson));

        // Act
        var result = await _bankService.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert
        Assert.True(result.Authorized);
        
        // Verify the request was made with correct body
        var logEntries = _wireMockServer.LogEntries.ToList();
        Assert.Single(logEntries);
        Assert.Contains("\"card_number\":\"4111111111111111\"", logEntries[0].RequestMessage.Body);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithConnectionTimeout_RetriesAndReturnsUnauthorized()
    {
        // Arrange
        var request = new BankPaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryDate = "12/25",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        _wireMockServer
            .Given(Request.Create()
                .WithPath("/payments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithDelay(TimeSpan.FromSeconds(10)) // Long delay to simulate timeout
                .WithStatusCode(HttpStatusCode.OK));

        // Configure HttpClient with short timeout
        _httpClient.Timeout = TimeSpan.FromSeconds(1);

        // Act
        var result = await _bankService.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert
        Assert.False(result.Authorized);
        Assert.Null(result.AuthorizationCode);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var request = new BankPaymentRequest
        {
            CardNumber = "4111111111111111",
            ExpiryDate = "12/25",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        _wireMockServer
            .Given(Request.Create()
                .WithPath("/payments")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithDelay(TimeSpan.FromSeconds(5))
                .WithStatusCode(HttpStatusCode.OK));

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // Act
        var result = await _bankService.ProcessPaymentAsync(request, cts.Token);

        // Assert
        Assert.False(result.Authorized);
        Assert.Null(result.AuthorizationCode);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _wireMockServer.Stop();
        _wireMockServer.Dispose();
    }
}

// FakeLogger implementation
public class FakeLogger<T> : ILogger<T>
{
    public List<LogEntry> LogEntries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) => null!;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, 
        Func<TState, Exception?, string> formatter)
    {
        LogEntries.Add(new LogEntry
        {
            LogLevel = logLevel,
            EventId = eventId,
            Message = formatter(state, exception),
            Exception = exception
        });
    }
}

public class LogEntry
{
    public LogLevel LogLevel { get; set; }
    public EventId EventId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
}