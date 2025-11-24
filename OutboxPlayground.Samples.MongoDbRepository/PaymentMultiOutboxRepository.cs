using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using OutboxPlayground.Infra.Abstractions;
using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.MongoDbRepository;

internal class PaymentMultiOutboxRepository : IPaymentRepository
{
    private readonly ILogger<PaymentMultiOutboxRepository> _logger;
    private readonly IMongoCollection<PaymentRequest> _payments;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<CloudEvent> _outbox;
    private readonly IMongoCollection<CloudEvent> _highRiskOutbox;
    private readonly IMongoDatabase _database;
    private readonly IRiskAssessmentService _riskAssessmentService;
    private readonly ICloudEventBuilder _eventBuilder;

    public PaymentMultiOutboxRepository(
        ILogger<PaymentMultiOutboxRepository> logger,
        IMongoDatabase database,
        IRiskAssessmentService riskAssessmentService,
        IDataSchemaProvider dataSchemaProvider)
    {
        _logger = logger;
        _database = database;
        _payments = database.GetCollection<PaymentRequest>("Payments");
        _users = database.GetCollection<User>("Users");
        _outbox = database.GetCollection<CloudEvent>("Outbox");
        _highRiskOutbox = database.GetCollection<CloudEvent>("HighRiskOutbox");
        _riskAssessmentService = riskAssessmentService;
        _eventBuilder = CloudEvent.CreateBuilder("MyBusinessDomain")
                                  .AddSchema(dataSchemaProvider)
                                  .AddType("PaymentCreated");
    }

    public async Task AddPaymentAsync(PaymentRequest payment, CancellationToken cancellationToken = default)
    {
        using var session = await _database.Client.StartSessionAsync(cancellationToken: cancellationToken);
        session.StartTransaction();

        try
        {
            // Business Logic
            await _payments.InsertOneAsync(session, payment, cancellationToken: cancellationToken);

            User user = new(payment.UserId, payment.UserName);
            await _users.InsertOneAsync(session, user, cancellationToken: cancellationToken);

            Risk risk = await _riskAssessmentService.AssessRiskAsync(payment, cancellationToken);
            PaymentMessage message = payment.ToMessage(risk);

            // Outbox Pattern - Standard outbox for all payments
            CloudEvent cloudEvent = await _eventBuilder.BuildAsync(message);
            await _outbox.InsertOneAsync(session, cloudEvent, cancellationToken: cancellationToken);

            // High Risk Outbox - Only for high-risk payments
            if (risk == Risk.High)
            {
                CloudEvent highRiskEvent = await _eventBuilder.BuildAsync(message);
                await _highRiskOutbox.InsertOneAsync(session, highRiskEvent, cancellationToken: cancellationToken);
            }

            await session.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await session.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }
}