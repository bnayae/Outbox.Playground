using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions;

[ExcludeFromCodeCoverage]
internal static class OtelRegistration
{
    public static WebApplicationBuilder AddOtel(this WebApplicationBuilder builder)
    {
        var appName = builder.Environment.ApplicationName;

        #region Logging

        ILoggingBuilder loggingBuilder = builder.Logging;

        loggingBuilder.AddOpenTelemetry(logging =>
        {
            var resource = ResourceBuilder.CreateDefault();
            logging.SetResourceBuilder(resource.AddService(appName));
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.AddOtlpExporter();
        });

        loggingBuilder.Configure(x =>
        {
            x.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
              | ActivityTrackingOptions.TraceId
              | ActivityTrackingOptions.Tags;
        });

        #endregion Logging

        var services = builder.Services;
        services.AddOpenTelemetry()
                .ConfigureResource(resource =>
                                   resource.AddService(appName)) // builder.Environment.ApplicationName
            .WithTracing(tracing =>
                tracing
                        .AddAspNetCoreInstrumentation(
                        o =>
                        {
                            o.AddDefaultNetCoreTraceFilters();
                            o.RecordException = true;
                        })
                        .AddHttpClientInstrumentation(o => o.AddDefaultHttpClientTraceFilters())
                        .SetSampler<AlwaysOnSampler>()
                        .AddOtlpExporter()
                        )
            .WithMetrics(meterBuilder =>
                meterBuilder
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .AddOtlpExporter()
                        );

        return builder;
    }

    private static void AddDefaultNetCoreTraceFilters(this AspNetCoreTraceInstrumentationOptions options)
    {
        options.Filter = (httpContext) =>
        {
            var path = httpContext.Request.Path;
            if (path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
                return false;
            if (path.StartsWithSegments("/_vs", StringComparison.OrdinalIgnoreCase))
                return false;
            if (path.StartsWithSegments("/_framework", StringComparison.OrdinalIgnoreCase))
                return false;
            return path != "/health" &&
                   path != "/favicon.ico" &&
                   path != "/metrics";
        };
    }

    private static void AddDefaultHttpClientTraceFilters(this HttpClientTraceInstrumentationOptions options)
    {
        options.FilterHttpRequestMessage = (request) =>
        {
            var path = request.RequestUri?.LocalPath;
            return !(path?.EndsWith("/getScriptTag", StringComparison.OrdinalIgnoreCase) ?? false);
        };
    }
}