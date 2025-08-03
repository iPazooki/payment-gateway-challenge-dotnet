namespace PaymentGateway.UnitTests.Tests.BankService;

public class BankServiceTests : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Infrastructure.Services.BankService _bankService;
    private readonly FakeLogger<Infrastructure.Services.BankService> _logger;

    public BankServiceTests()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _logger = new FakeLogger<Infrastructure.Services.BankService>();
        
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:8080/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        
        _httpClientFactory.CreateClient("BankApi").Returns(_httpClient);
        _bankService = new Infrastructure.Services.BankService(_httpClientFactory, _logger);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SendsCorrectRequestFormat()
    {
        // Arrange
        var request = new BankPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryDate = $"{DateTime.UtcNow.AddMonths(1).Month}/{DateTime.UtcNow.Year}",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        // Act
        BankPaymentResponse result = await _bankService.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Authorized);
        Assert.NotNull(result.AuthorizationCode);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ReturnsUnauthorizedResponse()
    {
        // Arrange - Using an expired date
        var request = new BankPaymentRequest
        {
            CardNumber = "2222405343248878",
            ExpiryDate = $"{DateTime.UtcNow.AddMonths(1).Month}/{DateTime.UtcNow.Year}",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        // Act
        var result = await _bankService.ProcessPaymentAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Authorized);
        Assert.NotNull(result.AuthorizationCode);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WithCancellationToken_RespectsCancellation()
    {
        // Arrange
        var request = new BankPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryDate = $"{DateTime.UtcNow.AddMonths(1).Month}/{DateTime.UtcNow.Year}",
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1)); 
        await cts.CancelAsync(); 

        // Act
        var result = await _bankService.ProcessPaymentAsync(request, cts.Token);

        // Assert
        Assert.False(result.Authorized);
        Assert.Null(result.AuthorizationCode);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}