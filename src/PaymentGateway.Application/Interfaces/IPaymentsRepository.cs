namespace PaymentGateway.Application.Interfaces;

/// <summary>
/// Repository interface for managing payment data operations.
/// Provides methods for storing, retrieving, and checking payment records.
/// </summary>
public interface IPaymentsRepository
{
    /// <summary>
    /// Adds a new payment record to the repository with an associated idempotency key.
    /// </summary>
    /// <param name="idempotencyKey">The idempotency key to prevent duplicate payment processing.</param>
    /// <param name="payment">The payment response object to store.</param>
    void Add(string idempotencyKey, PostPaymentResponse payment);

    /// <summary>
    /// Retrieves a payment record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the payment to retrieve.</param>
    /// <returns>The payment response object if found; otherwise, null.</returns>
    PostPaymentResponse? Get(Guid id);

    /// <summary>
    /// Checks if a payment with the specified idempotency key already exists.
    /// </summary>
    /// <param name="idempotencyKey">The idempotency key to check for existence.</param>
    /// <returns>True if a payment with the specified idempotency key exists; otherwise, false.</returns>
    bool IsItValidIdempotencyKey(string idempotencyKey);
}