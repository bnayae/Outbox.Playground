using System.Text.RegularExpressions;
using Vogen;

namespace OutboxPlayground.Infra.Abstractions;

[ValueObject<string>(Conversions.SystemTextJson | Conversions.TypeConverter, // Support for System.Text.Json and TypeConverter for a smooth serialization and deserialization
    toPrimitiveCasting: CastOperator.Implicit, // Implicit casting from Email to string
        fromPrimitiveCasting: CastOperator.Implicit)] // Implicit casting from string to Email
// [Instance("Empty", "00-00000000000000000000000000000000-0000000000000000-00")]
public readonly partial struct OtelTraceParent
{
    [GeneratedRegex(@"^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$",
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
                matchTimeoutMilliseconds: 500)]
    private static partial Regex Validator(); // compiled time generated validation

    /// <summary>
    /// ValidateAsync the input (convention based).
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static Validation Validate(string value) => Validator().IsMatch(value) ? Validation.Ok : Validation.Invalid("Invalid OpenTelemetry traceparent format.");
}

