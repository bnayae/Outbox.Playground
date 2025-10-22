using Microsoft.Extensions;
using OutboxPlayground.Infra.Abstractions;
using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.EFRepository;

internal class Paymentrepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;
    private readonly IRiskAssessmentService _riskAssessmentService;
    private readonly ICloudEventBuilder _eventBuilder;

    public Paymentrepository(PaymentDbContext context,
                             IRiskAssessmentService riskAssessmentService,
                             IDataSchemaProvider dataSchemaProvider)
    {
        _context = context;
        _riskAssessmentService = riskAssessmentService;
        _eventBuilder = CloudEvent.CreateBuilder("MyBusinessDomain")
                          .AddSchema(dataSchemaProvider)
                          .AddType("PaymentCreated");
    }

    async Task IPaymentRepository.AddPaymentAsync(Payment payment, CancellationToken cancellationToken)
    {
        using var activity = RepositoryOtelExtensions.ACTIVITY_SOURCE.StartActivity();
        _context.Payments.Add(payment);

        Risk risk = await _riskAssessmentService.AssessRiskAsync(payment, cancellationToken);
        PaymentMessage message  = payment.ToMessage(risk);
        CloudEvent cloudEvent = await _eventBuilder.BuildAsync(message);
        _context.Outbox.Add(cloudEvent);


        await _context.SaveChangesAsync(cancellationToken);
    }
}
