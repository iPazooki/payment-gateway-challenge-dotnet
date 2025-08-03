using FluentValidation;
using FluentValidation.Results;

namespace PaymentGateway.UnitTests.Tests.PaymentService;

public class PaymentServiceTests
{
    private readonly IBankService _bankService;
    private readonly FakeLogger<Infrastructure.Services.PaymentService> _logger;
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IValidator<PostPaymentRequest> _validator;
    private readonly Infrastructure.Services.PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _bankService = Substitute.For<IBankService>();
        _logger = new FakeLogger<Infrastructure.Services.PaymentService>();
        _paymentsRepository = Substitute.For<IPaymentsRepository>();
        _validator = Substitute.For<IValidator<PostPaymentRequest>>();
        
        _paymentService = new Infrastructure.Services.PaymentService(_bankService, _logger, _paymentsRepository, _validator);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenIdempotencyKeyIsNull_ReturnsRejectedPaymentWithError()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        string? idempotencyKey = null;
        var cancellationToken = CancellationToken.None;

        // Act
        var (response, errors) = await _paymentService.ProcessPaymentAsync(request, idempotencyKey, cancellationToken);

        // Assert
        Assert.Equal(PaymentStatus.Rejected, response.Status);
        Assert.Single(errors);
        Assert.Equal("Idempotency-Key header is required for payment requests", errors[0]);
        Assert.Equal(1234, response.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, response.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, response.ExpiryYear);
        Assert.Equal(request.Currency, response.Currency);
        Assert.Equal(request.Amount, response.Amount);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenIdempotencyKeyIsEmpty_ReturnsRejectedPaymentWithError()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var idempotencyKey = string.Empty;
        var cancellationToken = CancellationToken.None;

        // Act
        var (response, errors) = await _paymentService.ProcessPaymentAsync(request, idempotencyKey, cancellationToken);

        // Assert
        Assert.Equal(PaymentStatus.Rejected, response.Status);
        Assert.Single(errors);
        Assert.Equal("Idempotency-Key header is required for payment requests", errors[0]);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenIdempotencyKeyAlreadyUsed_ReturnsRejectedPaymentWithError()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var idempotencyKey = "test-key";
        var cancellationToken = CancellationToken.None;

        _paymentsRepository.GetByIdempotencyKey(idempotencyKey).Returns(true);

        // Act
        var (response, errors) = await _paymentService.ProcessPaymentAsync(request, idempotencyKey, cancellationToken);

        // Assert
        Assert.Equal(PaymentStatus.Rejected, response.Status);
        Assert.Single(errors);
        Assert.Equal("Idempotency-Key header already used for a previous payment", errors[0]);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenValidationFails_ReturnsRejectedPaymentWithValidationErrors()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var idempotencyKey = "test-key";
        var cancellationToken = CancellationToken.None;
        var validationErrors = new List<ValidationFailure>
        {
            new("CardNumber", "Card number is invalid"),
            new("Amount", "Amount must be greater than zero")
        };
        var validationResult = new ValidationResult(validationErrors);

        _paymentsRepository.GetByIdempotencyKey(idempotencyKey).Returns(false);
        _validator.ValidateAsync(request, cancellationToken).Returns(validationResult);

        // Act
        var (response, errors) = await _paymentService.ProcessPaymentAsync(request, idempotencyKey, cancellationToken);

        // Assert
        Assert.Equal(PaymentStatus.Rejected, response.Status);
        Assert.Equal(2, errors.Count);
        Assert.Contains("Card number is invalid", errors);
        Assert.Contains("Amount must be greater than zero", errors);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenBankAuthorizes_ReturnsAuthorizedPayment()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var idempotencyKey = "test-key";
        var cancellationToken = CancellationToken.None;
        var bankResponse = new BankPaymentResponse { Authorized = true };

        SetupSuccessfulValidation(request, cancellationToken);
        _paymentsRepository.GetByIdempotencyKey(idempotencyKey).Returns(false);
        _bankService.ProcessPaymentAsync(Arg.Any<BankPaymentRequest>(), cancellationToken).Returns(bankResponse);

        // Act
        var (response, errors) = await _paymentService.ProcessPaymentAsync(request, idempotencyKey, cancellationToken);

        // Assert
        Assert.Equal(PaymentStatus.Authorized, response.Status);
        Assert.Empty(errors);
        Assert.Equal(1234, response.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, response.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, response.ExpiryYear);
        Assert.Equal(request.Currency, response.Currency);
        Assert.Equal(request.Amount, response.Amount);
        Assert.NotEqual(Guid.Empty, response.Id);

        _paymentsRepository.Received(1).Add(idempotencyKey, response);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenBankDeclines_ReturnsDeclinedPayment()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var idempotencyKey = "test-key";
        var cancellationToken = CancellationToken.None;
        var bankResponse = new BankPaymentResponse { Authorized = false };

        SetupSuccessfulValidation(request, cancellationToken);
        _paymentsRepository.GetByIdempotencyKey(idempotencyKey).Returns(false);
        _bankService.ProcessPaymentAsync(Arg.Any<BankPaymentRequest>(), cancellationToken).Returns(bankResponse);

        // Act
        var (response, errors) = await _paymentService.ProcessPaymentAsync(request, idempotencyKey, cancellationToken);

        // Assert
        Assert.Equal(PaymentStatus.Declined, response.Status);
        Assert.Empty(errors);
        _paymentsRepository.Received(1).Add(idempotencyKey, response);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenBankServiceThrowsException_ReturnsRejectedPaymentWithBankError()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var idempotencyKey = "test-key";
        var cancellationToken = CancellationToken.None;
        var exception = new Exception("Bank service unavailable");

        SetupSuccessfulValidation(request, cancellationToken);
        _paymentsRepository.GetByIdempotencyKey(idempotencyKey).Returns(false);
        _bankService.ProcessPaymentAsync(Arg.Any<BankPaymentRequest>(), cancellationToken).ThrowsAsync(exception);

        // Act
        var (response, errors) = await _paymentService.ProcessPaymentAsync(request, idempotencyKey, cancellationToken);

        // Assert
        Assert.Equal(PaymentStatus.Rejected, response.Status);
        Assert.Single(errors);
        Assert.Equal("Bank service error", errors[0]);

        // Verify logging with real logger
        var logEntries = _logger.Collector.GetSnapshot();
        var errorLog = Assert.Single(logEntries.Where(log => log.Level == LogLevel.Error));
        Assert.Equal("Error processing payment", errorLog.Message);
        Assert.Equal(exception, errorLog.Exception);
    }

    [Fact]
    public async Task ProcessPaymentAsync_WhenCardNumberLessThan4Digits_ReturnsZeroLastFour()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = "123"; // Less than 4 digits
        var idempotencyKey = "test-key";
        var cancellationToken = CancellationToken.None;

        _paymentsRepository.GetByIdempotencyKey(idempotencyKey).Returns(false);

        var validationErrors = new List<ValidationFailure> { new("CardNumber", "Invalid card number") };
        var validationResult = new ValidationResult(validationErrors);
        _validator.ValidateAsync(request, cancellationToken).Returns(validationResult);

        // Act
        var (response, errors) = await _paymentService.ProcessPaymentAsync(request, idempotencyKey, cancellationToken);

        // Assert
        Assert.Equal(PaymentStatus.Rejected, response.Status);
        Assert.Equal(0, response.CardNumberLastFour);
    }

    [Fact]
    public void GetPayment_WhenPaymentExists_ReturnsGetPaymentResponse()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;
        var existingPayment = new PostPaymentResponse
        {
            Id = paymentId,
            Status = PaymentStatus.Authorized,
            CardNumberLastFour = 1234,
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 100
        };

        _paymentsRepository.Get(paymentId).Returns(existingPayment);

        // Act
        var result = _paymentService.GetPayment(paymentId, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(paymentId, result.Id);
        Assert.Equal(PaymentStatus.Authorized, result.Status);
        Assert.Equal(1234, result.CardNumberLastFour);
        Assert.Equal(12, result.ExpiryMonth);
        Assert.Equal(2025, result.ExpiryYear);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(100, result.Amount);
    }

    [Fact]
    public void GetPayment_WhenPaymentDoesNotExist_ReturnsNull()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        _paymentsRepository.Get(paymentId).Returns((PostPaymentResponse?)null);

        // Act
        var result = _paymentService.GetPayment(paymentId, cancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ProcessPaymentAsync_WhenCardNumberIsNullOrWhitespace_ReturnsZeroLastFour(string cardNumber)
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        request.CardNumber = cardNumber;
        var idempotencyKey = "test-key";
        var cancellationToken = CancellationToken.None;

        _paymentsRepository.GetByIdempotencyKey(idempotencyKey).Returns(false);

        var validationErrors = new List<ValidationFailure> { new("CardNumber", "Card number is required") };
        var validationResult = new ValidationResult(validationErrors);
        _validator.ValidateAsync(request, cancellationToken).Returns(validationResult);

        // Act
        var (response, errors) = await _paymentService.ProcessPaymentAsync(request, idempotencyKey, cancellationToken);

        // Assert
        Assert.Equal(PaymentStatus.Rejected, response.Status);
        Assert.Equal(0, response.CardNumberLastFour);
    }

    [Fact]
    public async Task ProcessPaymentAsync_SendsCorrectBankRequest()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var idempotencyKey = "test-key";
        var cancellationToken = CancellationToken.None;
        var bankResponse = new BankPaymentResponse { Authorized = true };

        SetupSuccessfulValidation(request, cancellationToken);
        _paymentsRepository.GetByIdempotencyKey(idempotencyKey).Returns(false);
        _bankService.ProcessPaymentAsync(Arg.Any<BankPaymentRequest>(), cancellationToken).Returns(bankResponse);

        // Act
        await _paymentService.ProcessPaymentAsync(request, idempotencyKey, cancellationToken);

        // Assert
        await _bankService.Received(1).ProcessPaymentAsync(
            Arg.Is<BankPaymentRequest>(br => 
                br.CardNumber == request.CardNumber &&
                br.ExpiryDate == $"{request.ExpiryMonth}/{request.ExpiryYear}" &&
                br.Currency == request.Currency &&
                br.Amount == request.Amount &&
                br.Cvv == request.Cvv),
            cancellationToken);
    }

    [Fact]
    public async Task ProcessPaymentAsync_LogsErrorWithCorrectLevel_WhenBankServiceFails()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var idempotencyKey = "test-key";
        var cancellationToken = CancellationToken.None;
        var exception = new InvalidOperationException("Specific bank error");

        SetupSuccessfulValidation(request, cancellationToken);
        _paymentsRepository.GetByIdempotencyKey(idempotencyKey).Returns(false);
        _bankService.ProcessPaymentAsync(Arg.Any<BankPaymentRequest>(), cancellationToken).ThrowsAsync(exception);

        // Act
        await _paymentService.ProcessPaymentAsync(request, idempotencyKey, cancellationToken);

        // Assert
        var logEntries = _logger.Collector.GetSnapshot();
        var errorLogs = logEntries.Where(log => log.Level == LogLevel.Error).ToList();
        
        Assert.Single(errorLogs);
        Assert.Equal("Error processing payment", errorLogs[0].Message);
        Assert.IsType<InvalidOperationException>(errorLogs[0].Exception);
        Assert.Equal("Specific bank error", errorLogs[0].Exception?.Message);
    }

    [Fact]
    public async Task ProcessPaymentAsync_DoesNotLog_WhenSuccessful()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var idempotencyKey = "test-key";
        var cancellationToken = CancellationToken.None;
        var bankResponse = new BankPaymentResponse { Authorized = true };

        SetupSuccessfulValidation(request, cancellationToken);
        _paymentsRepository.GetByIdempotencyKey(idempotencyKey).Returns(false);
        _bankService.ProcessPaymentAsync(Arg.Any<BankPaymentRequest>(), cancellationToken).Returns(bankResponse);

        // Act
        await _paymentService.ProcessPaymentAsync(request, idempotencyKey, cancellationToken);

        // Assert
        var logEntries = _logger.Collector.GetSnapshot();
        Assert.Empty(logEntries);
    }

    private static PostPaymentRequest CreateValidPaymentRequest()
    {
        return new PostPaymentRequest
        {
            CardNumber = "4111111111111234",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };
    }

    private void SetupSuccessfulValidation(PostPaymentRequest request, CancellationToken cancellationToken)
    {
        var validationResult = new ValidationResult();
        _validator.ValidateAsync(request, cancellationToken).Returns(validationResult);
    }
}