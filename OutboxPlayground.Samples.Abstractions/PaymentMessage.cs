using Avro;
using Avro.Specific;

namespace OutboxPlayground.Samples.Abstractions;


public record PaymentMessage : ISpecificRecord
{
    private static readonly Schema _SCHEMA = Schema.Parse(@"
    {
      ""type"": ""record"",
      ""name"": ""PaymentMessage"",
      ""namespace"": ""OutboxPlayground.Samples.Abstractions"",
      ""fields"": [
        { ""name"": ""Id"", ""type"": ""string"" },
        { ""name"": ""UserId"", ""type"": ""string"" },
        { ""name"": ""Amount"", ""type"": ""double"" },
        { ""name"": ""Currency"", ""type"": ""string"" },
        { ""name"": ""PaymentMethod"", ""type"": ""string"" },
        { ""name"": ""CustomerId"", ""type"": ""string"" },
        { ""name"": ""CreatedAt"", ""type"": ""long"" },
        {
          ""name"": ""Status"",
          ""type"": {
            ""type"": ""enum"",
            ""name"": ""PaymentStatus"",
            ""symbols"": [""Pending"", ""Processing"", ""Completed"", ""Failed"", ""Cancelled""]
          }
        },
        {
          ""name"": ""RiskAssessment"",
          ""type"": {
            ""type"": ""enum"",
            ""name"": ""Risk"",
            ""symbols"": [""Low"", ""Medium"", ""High""]
          }
        }
      ]
    }");

    Schema ISpecificRecord.Schema => _SCHEMA;

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public PaymentStatus Status { get; set; }
    public Risk RiskAssessment { get; set; }

    public PaymentMessage() { }

    public PaymentMessage(
        Guid Id,
        Guid UserId,
        decimal Amount,
        string Currency,
        string PaymentMethod,
        Guid CustomerId,
        DateTime CreatedAt,
        PaymentStatus Status,
        Risk RiskAssessment)
    {
        this.Id = Id;
        this.UserId = UserId;
        this.Amount = Amount;
        this.Currency = Currency;
        this.PaymentMethod = PaymentMethod;
        this.CustomerId = CustomerId;
        this.CreatedAt = CreatedAt;
        this.Status = Status;
        this.RiskAssessment = RiskAssessment;
    }

    object ISpecificRecord.Get(int fieldPos)
    {
        return fieldPos switch
        {
            0 => Id,
            1 => UserId,
            2 => Amount,
            3 => Currency,
            4 => PaymentMethod,
            5 => CustomerId,
            6 => CreatedAt,
            7 => Status,
            8 => RiskAssessment,
            _ => throw new AvroRuntimeException("Bad index " + fieldPos)
        };
    }

    void ISpecificRecord.Put(int fieldPos, object value)
    {
        switch (fieldPos)
        {
            case 0:
                Id = value switch
                {
                    Guid g => g,
                    string s => Guid.Parse(s),
                    _ => new Guid(Convert.ToString(value)!)
                };
                break;
            case 1:
                UserId = value switch
                {
                    Guid g => g,
                    string s => Guid.Parse(s),
                    _ => new Guid(Convert.ToString(value)!)
                };
                break;
            case 2:
                Amount = value switch
                {
                    decimal d => d,
                    double db => Convert.ToDecimal(db),
                    float f => Convert.ToDecimal(f),
                    string s => decimal.Parse(s),
                    _ => Convert.ToDecimal(value)
                };
                break;
            case 3:
                Currency = Convert.ToString(value) ?? string.Empty;
                break;
            case 4:
                PaymentMethod = Convert.ToString(value) ?? string.Empty;
                break;
            case 5:
                CustomerId = value switch
                {
                    Guid g => g,
                    string s => Guid.Parse(s),
                    _ => new Guid(Convert.ToString(value)!)
                };
                break;
            case 6:
                CreatedAt = value switch
                {
                    DateTime dt => dt,
                    long l => DateTimeOffset.FromUnixTimeMilliseconds(l).UtcDateTime,
                    int i => DateTimeOffset.FromUnixTimeMilliseconds(i).UtcDateTime,
                    string s => DateTime.Parse(s),
                    _ => DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(value)).UtcDateTime
                };
                break;
            case 7:
                Status = value switch
                {
                    PaymentStatus ps => ps,
                    string s => Enum.Parse<PaymentStatus>(s),
                    int i => (PaymentStatus)Convert.ToInt32(i),
                    _ => (PaymentStatus)Convert.ToInt32(value)
                };
                break;
            case 8:
                RiskAssessment = value switch
                {
                    Risk r => r,
                    string s => Enum.Parse<Risk>(s),
                    int i => (Risk)Convert.ToInt32(i),
                    _ => (Risk)Convert.ToInt32(value)
                };
                break;
            default:
                throw new AvroRuntimeException("Bad index " + fieldPos);
        }
    }
}
