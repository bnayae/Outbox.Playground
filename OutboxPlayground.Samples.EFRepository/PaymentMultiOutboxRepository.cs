using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions;
using Microsoft.Extensions.Logging;
using OutboxPlayground.Infra.Abstractions;
using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.EFRepository;

internal class PaymentMultiOutboxRepository : IPaymentRepository
{
    private readonly ILogger<PaymentMultiOutboxRepository> _logger;
    private readonly IDbContextFactory<PaymentDbMultiOutboxContext> _contextFactory;
    private readonly IRiskAssessmentService _riskAssessmentService;
    private readonly ICloudEventBuilder _eventBuilder;

    public PaymentMultiOutboxRepository(
                             ILogger<PaymentMultiOutboxRepository> logger,
                             IDbContextFactory<PaymentDbMultiOutboxContext> contextFactory,
                             IRiskAssessmentService riskAssessmentService,
                             IDataSchemaProvider dataSchemaProvider)
    {
        _logger = logger;
        _contextFactory = contextFactory;
        _riskAssessmentService = riskAssessmentService;
        _eventBuilder = CloudEvent.CreateBuilder("MyBusinessDomain")
                          .AddSchema(dataSchemaProvider)
                          .AddType("PaymentCreated");
    }

    async Task IPaymentRepository.AddPaymentAsync(PaymentRequest payment, CancellationToken cancellationToken)
    {
        PaymentDbMultiOutboxContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        using var activity = RepositoryOtelExtensions.ACTIVITY_SOURCE.StartActivity();

        PaymentEntity paymentEntity = payment.ToEntity();
        context.Payments.Add(paymentEntity);

        User user = new(payment.UserId, payment.UserName);
        context.Users.Add(user);

        Risk risk = await _riskAssessmentService.AssessRiskAsync(payment, cancellationToken);
        _logger.LogRisk(risk, payment.Id);
        PaymentMessage message  = payment.ToMessage(risk);
        CloudEvent cloudEvent = await _eventBuilder.BuildAsync(message);
        context.Outbox.Add(cloudEvent);

        if(risk == Risk.High)
        {
            CloudEvent highRiskEvent = await _eventBuilder.BuildAsync(message);
            context.HighRiskOutbox.Add(highRiskEvent);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
