namespace OutboxPlayground.Samples.Abstractions;

public enum PaymentStatus
{
    Pending,
    Processing,
    Completed,
    Failed,
    Cancelled
}
