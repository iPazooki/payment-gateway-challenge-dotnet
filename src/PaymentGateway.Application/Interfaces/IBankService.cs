namespace PaymentGateway.Application.Interfaces;

/// <summary>
/// Service interface for communicating with external banking systems.
/// Provides methods for processing payments through bank payment processors.
/// </summary>
public interface IBankService
{
    /// <summary>
    /// Processes a payment request through the external banking system asynchronously.
    /// </summary>
    /// <param name="request">The bank payment request containing payment details for the external bank processor.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the bank's response
    /// with payment processing status and transaction details.
    /// </returns>
    Task<BankPaymentResponse> ProcessPaymentAsync(BankPaymentRequest request, CancellationToken cancellationToken);
}