using System.Collections.Concurrent;

namespace PaymentGateway.Infrastructure.Persistence.Services;

internal class PaymentsRepository: IPaymentsRepository
{
    private readonly ConcurrentBag<PostPaymentResponse> _payments = [];
    private readonly ConcurrentBag<string> _idempotencyKeys = [];
    
    public void Add(string idempotencyKey, PostPaymentResponse payment)
    {
        _payments.Add(payment);
        _idempotencyKeys.Add(idempotencyKey);
    }

    public PostPaymentResponse? Get(Guid id)
    {
        return _payments.FirstOrDefault(p => p.Id == id);
    }

    public bool IsItValidIdempotencyKey(string idempotencyKey)
    {
        return !_idempotencyKeys.Contains(idempotencyKey, StringComparer.InvariantCultureIgnoreCase);
    }
}