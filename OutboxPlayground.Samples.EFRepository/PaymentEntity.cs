namespace OutboxPlayground.Samples.Abstractions;

internal record PaymentEntity(
    Guid Id,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    Guid CustomerId,
    DateTime CreatedAt,
    PaymentStatus Status
);
