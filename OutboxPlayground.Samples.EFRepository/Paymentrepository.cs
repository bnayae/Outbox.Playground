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

        // ADD Outbox

        // _context.Outbox.Add(Infra.Abstractions.CloudEvent.Create();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
