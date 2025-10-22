namespace OutboxPlayground.Samples.Jobs;

internal static partial class Logs
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Processing a message: Message Type = {ceType}, Timestamp = {ceTime}, Content Type = {contentType}")]
    public static partial void ProcessingMessage(this ILogger logger, string? ceType, string? ceTime, string? contentType);
}
