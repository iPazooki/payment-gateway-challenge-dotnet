namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests : BaseIntegrationTest
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public PaymentsControllerTests()
    {
        var factory = new IntegrationWebApplicationFactory();
        _httpClient = factory.CreateClient();

        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    #region PostPaymentAsync Tests

    [Fact]
    public async Task PostPaymentAsync_WithValidRequest_ShouldReturnOkWithPaymentResponse()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 9,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
        var idempotencyKey = Guid.NewGuid().ToString();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments");
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var paymentResponse = JsonSerializer.Deserialize<PostPaymentResponse>(content, _jsonOptions);

        Assert.NotNull(paymentResponse);
        Assert.True(paymentResponse.Status == PaymentStatus.Authorized);
        Assert.NotEqual(Guid.Empty, paymentResponse.Id);
    }
    
    [Fact]
    public async Task PostPaymentAsync_NotWithValidRequest_ShouldReturnOkWithPaymentDeclineResponse()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248878",
            ExpiryMonth = 9,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
        var idempotencyKey = Guid.NewGuid().ToString();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments");
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var paymentResponse = JsonSerializer.Deserialize<PostPaymentResponse>(content, _jsonOptions);

        Assert.NotNull(paymentResponse);
        Assert.True(paymentResponse.Status == PaymentStatus.Declined);
        Assert.NotEqual(Guid.Empty, paymentResponse.Id);
    }

    [Fact]
    public async Task PostPaymentAsync_WithMissingIdempotencyKey_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 9,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/v1/payments", request, _jsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPaymentAsync_WithExpiredCard_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 1,
            ExpiryYear = 2020, // Expired year
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
        var idempotencyKey = Guid.NewGuid().ToString();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments");
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPaymentAsync_WithInvalidAmount_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 9,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 0, // Invalid amount
            Cvv = "123"
        };
        var idempotencyKey = Guid.NewGuid().ToString();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments");
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPaymentAsync_WithInvalidCurrency_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 9,
            ExpiryYear = 2025,
            Currency = "INVALID", // Invalid currency
            Amount = 100,
            Cvv = "123"
        };
        var idempotencyKey = Guid.NewGuid().ToString();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments");
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPaymentAsync_WithDuplicateIdempotencyKey_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 9,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
        var idempotencyKey = Guid.NewGuid().ToString();

        using var firstRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments");
        firstRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        firstRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        using var secondRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments");
        secondRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        secondRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var firstResponse = await _httpClient.SendAsync(firstRequest);
        var secondResponse = await _httpClient.SendAsync(secondRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, secondResponse.StatusCode);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    public async Task PostPaymentAsync_WithValidCurrencies_ShouldReturnOk(string currency)
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 9,
            ExpiryYear = 2025,
            Currency = currency,
            Amount = 100,
            Cvv = "123"
        };
        var idempotencyKey = Guid.NewGuid().ToString();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments");
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region GetPayment Tests

    [Fact]
    public async Task GetPayment_WithExistingPaymentId_ShouldReturnOkWithPaymentDetails()
    {
        // Arrange - First create a payment
        var postRequest = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 9,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
        var idempotencyKey = Guid.NewGuid().ToString();

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments");
        createRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        createRequest.Content = JsonContent.Create(postRequest, options: _jsonOptions);

        var createResponse = await _httpClient.SendAsync(createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createdPayment = JsonSerializer.Deserialize<PostPaymentResponse>(createContent, _jsonOptions);

        // Act - Retrieve the payment
        var getResponse = await _httpClient.GetAsync($"/api/v1/payments/{createdPayment.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var getContent = await getResponse.Content.ReadAsStringAsync();
        var retrievedPayment = JsonSerializer.Deserialize<GetPaymentResponse>(getContent, _jsonOptions);

        Assert.NotNull(retrievedPayment);
        Assert.Equal(createdPayment.Id, retrievedPayment.Id);
        Assert.Equal(8877, retrievedPayment.CardNumberLastFour); // Last 4 digits of test card
        Assert.Equal(9, retrievedPayment.ExpiryMonth);
        Assert.Equal(2025, retrievedPayment.ExpiryYear);
        Assert.Equal("GBP", retrievedPayment.Currency);
        Assert.Equal(100, retrievedPayment.Amount);
        Assert.True(retrievedPayment.Status == PaymentStatus.Authorized ||
                    retrievedPayment.Status == PaymentStatus.Declined);
    }

    [Fact]
    public async Task GetPayment_WithNonExistentPaymentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _httpClient.GetAsync($"/api/v1/payments/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region API Versioning Tests

    [Fact]
    public async Task PostPaymentAsync_WithVersionInUrl_ShouldReturnOk()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 9,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
        var idempotencyKey = Guid.NewGuid().ToString();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1.0/payments");
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPayment_WithVersionInUrl_ShouldWork()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        // Act
        var response = await _httpClient.GetAsync($"/api/v1.0/payments/{paymentId}");

        // Assert
        // Should return 404 (not found) rather than version error, indicating versioning works
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Content Type and Headers Tests

    [Fact]
    public async Task PostPaymentAsync_WithoutContentType_ShouldReturnUnsupportedMediaType()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var requestContent =
            "{\"cardNumber\":\"2222405343248877\",\"expiryMonth\":9,\"expiryYear\":2025,\"currency\":\"GBP\",\"amount\":100,\"cvv\":\"123\"}";

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments");
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        httpRequest.Content = new StringContent(requestContent);

        // Act
        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task PaymentsController_ShouldReturnJsonContentType()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 9,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
        var idempotencyKey = Guid.NewGuid().ToString();

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments");
        httpRequest.Headers.Add("Idempotency-Key", idempotencyKey);
        httpRequest.Content = JsonContent.Create(request, options: _jsonOptions);

        // Act
        var response = await _httpClient.SendAsync(httpRequest);

        // Assert
        Assert.Contains("application/json", response.Content.Headers.ContentType?.ToString());
    }

    #endregion
}