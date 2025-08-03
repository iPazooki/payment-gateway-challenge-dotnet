using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Models.Responses;

/// <summary>
/// Represents the response model returned when retrieving payment details by ID.
/// Contains payment information with sensitive card data masked for security.
/// </summary>
public sealed class GetPaymentResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the payment transaction.
    /// </summary>
    /// <value>A GUID that uniquely identifies the payment in the system.</value>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets or sets the current status of the payment.
    /// </summary>
    /// <value>The payment status indicating whether it was authorized, declined, or rejected.</value>
    public PaymentStatus Status { get; init; }

    /// <summary>
    /// Gets or sets the last four digits of the card number used for the payment.
    /// </summary>
    /// <value>An integer representing the last four digits of the card number for identification purposes.</value>
    /// <remarks>Only the last four digits are stored for security and PCI compliance.</remarks>
    public int CardNumberLastFour { get; init; }

    /// <summary>
    /// Gets or sets the expiry month of the card used for the payment.
    /// </summary>
    /// <value>An integer between 1 and 12 representing the card's expiry month.</value>
    public int ExpiryMonth { get; init; }

    /// <summary>
    /// Gets or sets the expiry year of the card used for the payment.
    /// </summary>
    /// <value>A four-digit integer representing the card's expiry year.</value>
    public int ExpiryYear { get; init; }

    /// <summary>
    /// Gets or sets the currency code for the payment amount.
    /// </summary>
    /// <value>A three-letter ISO 4217 currency code (e.g., "USD", "EUR", "GBP").</value>
    public required string Currency { get; init; } 

    /// <summary>
    /// Gets or sets the payment amount in the smallest currency unit.
    /// </summary>
    /// <value>The amount in cents/pence (e.g., 1500 represents $15.00 or £15.00).</value>
    /// <remarks>Amount is stored in the smallest currency unit to avoid floating-point precision issues.</remarks>
    public int Amount { get; init; }
}