using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions;

[ExcludeFromCodeCoverage]
internal static class RepositoryOtelExtensions
{
    public const string ACTIVITY_SOURCE_NAME = "Outbox.Sample";

    public static ActivitySource ACTIVITY_SOURCE = new ActivitySource(ACTIVITY_SOURCE_NAME);

    public static TracerProviderBuilder AddSampleInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(ACTIVITY_SOURCE_NAME);
    }
}