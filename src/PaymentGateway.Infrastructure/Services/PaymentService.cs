using FluentValidation;

namespace PaymentGateway.Infrastructure.Services;

internal class PaymentService(
    IBankService bankService,
    ILogger<PaymentService> logger,
    IPaymentsRepository paymentsRepository,
    IValidator<PostPaymentRequest> validator) : IPaymentService
{
    public async Task<(PostPaymentResponse response, List<string> errors)> ProcessPaymentAsync(PostPaymentRequest request, string? idempotencyKey, CancellationToken cancellationToken)
    {
        PostPaymentResponse rejectedPayment = CreateRejectedPayment(request);
        
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return (rejectedPayment, ["Idempotency-Key header is required for payment requests"]);
        }
        
        var validIdempotencyKey = paymentsRepository.IsItValidIdempotencyKey(idempotencyKey);
        
        if (!validIdempotencyKey)
        {
            return (rejectedPayment, ["Idempotency-Key header already used for a previous payment"]);
        }
        
        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            List<string> validationErrors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            
            return (rejectedPayment, validationErrors);
        }

        try
        {
            BankPaymentRequest bankRequest = new()
            {
                CardNumber = request.CardNumber,
                ExpiryDate = $"{request.ExpiryMonth}/{request.ExpiryYear}",
                Currency = request.Currency,
                Amount = request.Amount,
                Cvv = request.Cvv
            };
            
            BankPaymentResponse bankResponse = await bankService.ProcessPaymentAsync(bankRequest, cancellationToken);
            
            PostPaymentResponse payment = new()
            {
                Id = Guid.NewGuid(),
                Status = bankResponse.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
                CardNumberLastFour = int.Parse(request.CardNumber.Substring(request.CardNumber.Length - 4)),
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency,
                Amount = request.Amount
            };

            paymentsRepository.Add(idempotencyKey, payment);
            return (payment, []);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing payment");

            List<string> bankErrors = ["Bank service error"];
            
            return (rejectedPayment, bankErrors);
        }
    }

    public GetPaymentResponse? GetPayment(Guid id, CancellationToken cancellationToken)
    {
        PostPaymentResponse? payment = paymentsRepository.Get(id);

        if (payment == null)
        {
            return null;
        }

        return new GetPaymentResponse
        {
            Id = payment.Id,
            Status = payment.Status,
            CardNumberLastFour = payment.CardNumberLastFour,
            ExpiryMonth = payment.ExpiryMonth,
            ExpiryYear = payment.ExpiryYear,
            Currency = payment.Currency,
            Amount = payment.Amount
        };
    }

    private PostPaymentResponse CreateRejectedPayment(PostPaymentRequest request)
    {
        // For rejected payments, we still need to extract what data we can
        int lastFour = 0;
        if (!string.IsNullOrWhiteSpace(request.CardNumber) && request.CardNumber.Length >= 4)
        {
            if (int.TryParse(request.CardNumber.AsSpan(request.CardNumber.Length - 4), out int parsed))
            {
                lastFour = parsed;
            }
        }

        return new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Rejected,
            CardNumberLastFour = lastFour,
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount
        };
    }
}