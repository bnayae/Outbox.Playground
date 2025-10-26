using Microsoft.Extensions.Logging;
using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.EFRepository;

internal static partial class Logs
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Assessed risk {Risk} of payment [{PaymentId}]")]
    public static partial void LogRisk(this ILogger logger, Risk risk, Guid paymentId);
}
