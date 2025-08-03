using System.Text.Json.Serialization;

namespace PaymentGateway.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentStatus
{
    Authorized,
    Declined,
    Rejected
}