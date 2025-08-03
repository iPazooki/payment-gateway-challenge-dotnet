using System.Text.Json.Serialization;

namespace PaymentGateway.Domain.Models.Requests;

public sealed class BankPaymentRequest
{
    [JsonPropertyName("card_number")] 
    public required string CardNumber { get; init; }

    [JsonPropertyName("expiry_date")] 
    public required string ExpiryDate { get; init; }

    [JsonPropertyName("currency")] 
    public required string Currency { get; init; }

    [JsonPropertyName("amount")] 
    public required int Amount { get; init; }

    [JsonPropertyName("cvv")] 
    public required string Cvv { get; init; }
}