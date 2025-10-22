using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions;

[ExcludeFromCodeCoverage]
internal static class OtelExtensions
{
    public const string ACTIVITY_SOURCE_NAME = "Outbox.Job";

    public static ActivitySource ACTIVITY_SOURCE = new ActivitySource(ACTIVITY_SOURCE_NAME);

    public static TracerProviderBuilder AddJobInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(ACTIVITY_SOURCE_NAME);
    }

    public static WebApplicationBuilder AddOtel(this WebApplicationBuilder builder)
    {
        var appName = builder.Environment.ApplicationName;
        var aspireEndpoint = GetAspireDashboardEndpoint();

        #region Logging

        ILoggingBuilder loggingBuilder = builder.Logging;

        loggingBuilder.AddOpenTelemetry(logging =>
        {
            var resource = ResourceBuilder.CreateDefault();
            logging.SetResourceBuilder(resource.AddService(appName));
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            
            if (!string.IsNullOrEmpty(aspireEndpoint))
            {
                logging.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(aspireEndpoint);
                });
            }
            else
            {
                logging.AddOtlpExporter();
            }
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
                        .AddJobInstrumentation()
                        .AddAspNetCoreInstrumentation(
                        o =>
                        {
                            o.AddDefaultNetCoreTraceFilters();
                            o.RecordException = true;
                        })
                        .AddHttpClientInstrumentation(o => o.AddDefaultHttpClientTraceFilters())
                        .AddSqlClientInstrumentation(o =>
                        {
                            o.RecordException = true;
                        })
                        .SetSampler<AlwaysOnSampler>()
                        .AddOtlpExporter(options =>
                        {
                            if (!string.IsNullOrEmpty(aspireEndpoint))
                            {
                                options.Endpoint = new Uri(aspireEndpoint);
                            }
                        })
                        )
            .WithMetrics(meterBuilder =>
                meterBuilder
                            .AddAspNetCoreInstrumentation()
                            .AddHttpClientInstrumentation()
                            .AddOtlpExporter(options =>
                            {
                                if (!string.IsNullOrEmpty(aspireEndpoint))
                                {
                                    options.Endpoint = new Uri(aspireEndpoint);
                                }
                            })
                        );

        return builder;
    }

    private static string? GetAspireDashboardEndpoint()
    {
        // Check for Aspire dashboard endpoint in environment variables
        // Aspire typically uses this environment variable
        return Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ??
               Environment.GetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_ENDPOINT") ??
               // Default Aspire dashboard endpoint for local development
               (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == null ? 
                "http://localhost:18889" : null);
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