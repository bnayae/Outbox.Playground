namespace OutboxPlayground.Samples.Abstractions;

public interface IPaymentRepository
{
    Task AddPaymentAsync(PaymentRequest payment, CancellationToken cancellationToken = default);
}