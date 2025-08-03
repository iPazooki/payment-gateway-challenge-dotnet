namespace PaymentGateway.Application.Interfaces;

public interface IPaymentService
{
    Task<(PostPaymentResponse response, List<string> errors)> ProcessPaymentAsync(PostPaymentRequest request, string? idempotencyKey, CancellationToken cancellationToken);
    GetPaymentResponse? GetPayment(Guid id, CancellationToken cancellationToken);
}
