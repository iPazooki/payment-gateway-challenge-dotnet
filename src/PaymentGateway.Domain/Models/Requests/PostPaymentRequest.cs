namespace PaymentGateway.Domain.Models.Requests;

/// <summary>
/// Request model for processing a payment transaction
/// </summary>
/// <example>
/// {
///   "cardNumber": "2222405343248877",
///   "expiryMonth": 9,
///   "expiryYear": 2025,
///   "currency": "GBP",
///   "amount": 100,
///   "cvv": "123"
/// }
/// </example>
public class PostPaymentRequest
{
    /// <summary>
    /// The payment card number (16 digits for most cards)
    /// </summary>
    /// <example>2222405343248877</example>
    public string CardNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// The expiry month of the card (1-12)
    /// </summary>
    /// <example>9</example>
    public int ExpiryMonth { get; set; }
    
    /// <summary>
    /// The expiry year of the card (4 digits)
    /// </summary>
    /// <example>2025</example>
    public int ExpiryYear { get; set; }
    
    
    /// <summary>
    /// The currency code for the transaction (ISO 4217 format)
    /// </summary>
    /// <example>GBP</example>
    /// <seealso href="https://www.xe.com/iso4217.php">ISO 4217 Currency Codes</seealso>
    public string Currency { get; set; } = string.Empty;
    
    /// <summary>
    /// The payment amount in the smallest currency unit (e.g., cents for USD)
    /// </summary>
    /// <example>1999</example>
    /// <remarks>
    /// For USD: 1 = $0.01, For EUR: 1050 = €10.50
    /// </remarks>
    public int Amount { get; set; }
    
    /// <summary>
    /// The Card Verification Value (CVV) - 3 or 4 digit security code
    /// </summary>
    /// <example>123</example>
    public string Cvv { get; set; } = string.Empty;
}