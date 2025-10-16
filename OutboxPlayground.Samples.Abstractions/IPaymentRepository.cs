namespace OutboxPlayground.Samples.Abstractions;

public interface IPaymentRepository
{
    Task AddPaymentAsync(Payment payment, CancellationToken cancellationToken = default);
}