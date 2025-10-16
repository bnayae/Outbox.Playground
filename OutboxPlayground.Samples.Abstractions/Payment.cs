namespace OutboxPlayground.Samples.Abstractions;


public record Payment(
    Guid Id,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    Guid CustomerId,
    DateTime CreatedAt,
    PaymentStatus Status
);
