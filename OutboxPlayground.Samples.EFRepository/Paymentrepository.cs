using OutboxPlayground.Infra.Abstractions;
using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.EFRepository;

internal class Paymentrepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public Paymentrepository(PaymentDbContext context)
    {
        _context = context;
    }

    async Task IPaymentRepository.AddPaymentAsync(Payment payment, CancellationToken cancellationToken)
    {
        _context.Payments.Add(payment);

        _context.Outbox.Add(CloudEvent.Create(Guid.NewGuid().ToString(),"paymentservice", System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(payment), "PaymentCreated"));

        await _context.SaveChangesAsync(cancellationToken);
    }
}
