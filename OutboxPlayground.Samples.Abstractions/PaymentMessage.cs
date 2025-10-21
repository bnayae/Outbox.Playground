namespace OutboxPlayground.Samples.Abstractions;


public record PaymentMessage(
    Guid Id,
    decimal Amount,
    string Currency,
    string PaymentMethod,
    Guid CustomerId,
    DateTime CreatedAt,
    PaymentStatus Status,
    Risk RiskAssessment
);
