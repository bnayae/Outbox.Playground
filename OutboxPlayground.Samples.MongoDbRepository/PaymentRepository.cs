using MongoDB.Driver;
using OutboxPlayground.Infra.Abstractions;
using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.MongoDbRepository;
internal class PaymentRepository : IPaymentRepository
{
    private readonly IMongoCollection<PaymentRequest> _payments;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<CloudEvent> _outbox;
    private readonly IMongoDatabase _database;
    private readonly IRiskAssessmentService _riskAssessmentService;
    private readonly ICloudEventBuilder _eventBuilder;

    public PaymentRepository(
        IMongoDatabase database,
        IRiskAssessmentService riskAssessmentService,
        IDataSchemaProvider dataSchemaProvider)
    {
        _database = database;
        _payments = database.GetCollection<PaymentRequest>("payments");
        _users = database.GetCollection<User>("users");
        _outbox = database.GetCollection<CloudEvent>("outbox");
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

            // Outbox Pattern
            CloudEvent cloudEvent = await _eventBuilder
                .AddPartition(payment.CustomerId)
                .BuildAsync(payment.Id, message);
            await _outbox.InsertOneAsync(session, cloudEvent, cancellationToken: cancellationToken);

            await session.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await session.AbortTransactionAsync(cancellationToken);
            throw;
        }
    }
}
