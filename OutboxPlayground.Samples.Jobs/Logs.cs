using OutboxPlayground.Samples.Abstractions;

namespace OutboxPlayground.Samples.Jobs;

internal static partial class Logs
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Processing a message: Message Type = {ceType}, Timestamp = {ceTime}, Content Type = {contentType}")]
    public static partial void LogProcessingMessage(this ILogger logger, string? ceType, string? ceTime, string? contentType);
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "message Data: {payment}")]
    public static partial void LogMessageData(this ILogger logger, [LogProperties] PaymentMessage? payment);
}
