namespace PaymentGateway.Application.Interfaces;

/// <summary>
/// Service interface for handling payment operations.
/// Provides methods for processing payments and retrieving payment information.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Processes a payment request asynchronously with optional idempotency support.
    /// </summary>
    /// <param name="request">The payment request containing payment details.</param>
    /// <param name="idempotencyKey">Optional idempotency key to prevent duplicate payment processing. Can be null.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a tuple with:
    /// - response: The payment response if successful
    /// - errors: A list of validation or processing errors, if any
    /// </returns>
    Task<(PostPaymentResponse response, List<string> errors)> ProcessPaymentAsync(PostPaymentRequest request, string? idempotencyKey, CancellationToken cancellationToken);
    
    /// <summary>
    /// Retrieves payment information by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the payment to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation if needed.</param>
    /// <returns>The payment response object if found; otherwise, null.</returns>
    GetPaymentResponse? GetPayment(Guid id, CancellationToken cancellationToken);
}