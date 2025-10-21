using OutboxPlayground.Infra.Abstractions;
using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.EFRepository;

internal class Paymentrepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;
    private readonly ICloudEventBuilder _eventBuilder;

    public Paymentrepository(PaymentDbContext context, IDataSchemaProvider dataSchemaProvider)
    {
        _context = context;
        _eventBuilder = CloudEvent.CreateBuilder("MyBusinessDomain")
                          .AddSchema(dataSchemaProvider)
                          .AddType("PaymentCreated");
    }

    async Task IPaymentRepository.AddPaymentAsync(Payment payment, CancellationToken cancellationToken)
    {
        _context.Payments.Add(payment);

        CloudEvent cloudEvent = await _eventBuilder.BuildAsync(payment);
        var entity = cloudEvent; //.ToEntity();
        _context.Outbox.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
