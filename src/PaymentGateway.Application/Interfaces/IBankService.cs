namespace PaymentGateway.Application.Interfaces;

public interface IBankService
{
    Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request, CancellationToken cancellationToken);
}