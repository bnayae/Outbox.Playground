namespace OutboxPlayground.Samples.Abstractions;


public record PaymentRequest(
    Guid Id,
    Guid UserId,
    string UserName,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    Guid CustomerId,
    DateTime CreatedAt,
    PaymentStatus Status
);
