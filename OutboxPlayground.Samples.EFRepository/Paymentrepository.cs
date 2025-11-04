using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions;
using OutboxPlayground.Infra.Abstractions;
using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.EFRepository;

internal class PaymentRepository : IPaymentRepository
{
    private readonly IDbContextFactory<PaymentDbContext> _contextFactory;
    private readonly IRiskAssessmentService _riskAssessmentService;
    private readonly ICloudEventBuilder _eventBuilder;

    public PaymentRepository(IDbContextFactory<PaymentDbContext> contextFactory,
                             IRiskAssessmentService riskAssessmentService,
                             IDataSchemaProvider dataSchemaProvider)
    {
        _contextFactory = contextFactory;
        _riskAssessmentService = riskAssessmentService;
        _eventBuilder = CloudEvent.CreateBuilder("MyBusinessDomain")
                          .AddSchema(dataSchemaProvider)
                          .AddType("PaymentCreated");
    }

    async Task IPaymentRepository.AddPaymentAsync(PaymentRequest payment, CancellationToken cancellationToken)
    {
        using var activity = RepositoryOtelExtensions.ACTIVITY_SOURCE.StartActivity(); // OTEL Tracing
        PaymentDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken); // EF Context

        // Bussiness Logic
        PaymentEntity paymentEntity = payment.ToEntity();
        context.Payments.Add(paymentEntity);

        User user = new(payment.UserId, payment.UserName);
        context.Users.Add(user);

        Risk risk = await _riskAssessmentService.AssessRiskAsync(payment, cancellationToken);
        PaymentMessage message = payment.ToMessage(risk);

        // Outbox Pattern
        CloudEvent cloudEvent = await _eventBuilder
                                            .AddPartition(payment.CustomerId)
                                            .BuildAsync(payment.Id, message);
        context.Outbox.Add(cloudEvent);
        // End outbox pattern

        // EF practice to save all changes in a single transaction

        await context.SaveChangesAsync(cancellationToken);
    }
}
