namespace OutboxPlayground.Samples.Abstractions;

public interface IRiskAssessmentService
{
    Task<Risk> AssessRiskAsync(PaymentRequest payment, CancellationToken cancellationToken = default);
}
