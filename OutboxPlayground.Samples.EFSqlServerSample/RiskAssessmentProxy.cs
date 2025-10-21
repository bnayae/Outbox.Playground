using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.EFSqlServerSample;

public class RiskAssessmentProxy : IRiskAssessmentService
{
    async Task<Risk> IRiskAssessmentService.AssessRiskAsync(Payment payment, CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken); // Simulate some latency
        return (Risk)(Environment.TickCount % 3);
    }
}
