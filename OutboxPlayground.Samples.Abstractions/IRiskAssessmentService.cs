namespace OutboxPlayground.Samples.Abstractions;

public interface IRiskAssessmentService
{
    Task<Risk> AssessRiskAsync(Payment payment, CancellationToken cancellationToken = default);
}
