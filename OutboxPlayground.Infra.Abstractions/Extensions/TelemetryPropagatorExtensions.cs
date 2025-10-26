using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace OutboxPlayground.Infra.Abstractions;

public static class TelemetryPropagatorExtensions
{
    #region SerializeTelemetryContext

    /// <summary>
    /// Extract context from Activity into Json
    /// </summary>
    /// <param name="activity"></param>
    /// <param name="propagator"></param>
    /// <returns></returns>
    public static OtelTraceParent? SerializeTelemetryContext(this Activity activity, TextMapPropagator? propagator = null)
    {
        if (activity.Context == default)
            return null;

        if (activity.Context.TraceId == default)
            return null;
        if (activity.Context.SpanId == default)
            return null;

        string flags = activity.Context.TraceFlags.HasFlag(ActivityTraceFlags.Recorded) ? "01" : "00";
        string trace = activity.Context.TraceId.ToHexString();
        string span = activity.Context.SpanId.ToHexString();

        // 00-<trace-id>-<span-id>-<trace-flags> 
        // flags: 0x01 = sampled, 0x00 = not sampled
        OtelTraceParent traceParent = $"00-{trace}-{span}-{flags}";
        return traceParent;
    }

    #endregion //  SerializeTelemetryContext

    #region ToTelemetryContext

    /// <summary>
    /// Extract OtelTraceParent into OTEL context
    /// </summary>
    /// <param name="traceParent"></param>
    /// <param name="propagator"></param>
    /// <returns></returns>
    public static ActivityContext ToTelemetryContext(this OtelTraceParent? traceParent, TextMapPropagator? propagator = null)
    {
        if (traceParent is null || traceParent.Value == OtelTraceParent.Empty || string.IsNullOrEmpty(traceParent))
            return default;

        return ActivityContext.TryParse(
            traceParent,
            null, // No trace state
            out ActivityContext activityContext) ?
            activityContext :
            default;
    }

    #endregion //  ToTelemetryContex
}
