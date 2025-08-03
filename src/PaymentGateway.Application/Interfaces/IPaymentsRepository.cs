namespace PaymentGateway.Application.Interfaces;

public interface IPaymentsRepository
{
    void Add(string idempotencyKey, PostPaymentResponse payment);

    PostPaymentResponse? Get(Guid id);

    bool GetByIdempotencyKey(string idempotencyKey);
}