using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Domain.Models.Responses;

/// <summary>
/// Response model for payment processing requests
/// </summary>
public sealed class PostPaymentResponse
{
    /// <summary>
    /// Unique identifier for the payment transaction
    /// </summary>
    /// <example>550e8400-e29b-41d4-a716-446655440000</example>
    public Guid Id { get; init; }
    
    /// <summary>
    /// Payment processing status indicating the outcome of the transaction
    /// </summary>
    /// <example>Authorized</example>
    public PaymentStatus Status { get; init; }
    
    /// <summary>
    /// Last four digits of the card number used for the payment
    /// </summary>
    /// <example>1234</example>
    public int CardNumberLastFour { get; init; }
    
    /// <summary>
    /// Expiry month of the card (1-12)
    /// </summary>
    /// <example>12</example>
    public int ExpiryMonth { get; init; }
    
    /// <summary>
    /// Expiry year of the card
    /// </summary>
    /// <example>2025</example>
    public int ExpiryYear { get; init; }
    
    /// <summary>
    /// Currency code for the payment (3-letter ISO currency code)
    /// </summary>
    /// <example>USD</example>
    public required string Currency { get; init; }
    
    /// <summary>
    /// Payment amount in the smallest currency unit (e.g., cents for USD)
    /// </summary>
    /// <example>1000</example>
    public int Amount { get; init; }
}