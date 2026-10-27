using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Instrumentation.EntityFrameworkCore;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Instrumentation.SqlClient;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Portal.Services.Observability
{
    /// <summary>
    /// Extensions for configuring OpenTelemetry with traces, metrics, and logs.
    /// Supports Console (development) and OTLP (production) exporters.
    /// </summary>
    public static class OpenTelemetrySetup
    {
        public static WebApplicationBuilder AddPortalOpenTelemetry(this WebApplicationBuilder builder)
        {
            var environment = builder.Environment;
            var configuration = builder.Configuration;
            var serviceVersion = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0";

            // Get OpenTelemetry configuration from appsettings
            var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
            var enabled = configuration.GetValue<bool>("OpenTelemetry:Enabled");
            var consoleExporter = configuration.GetValue<bool>("OpenTelemetry:ConsoleExporter");
            var samplingRatio = configuration.GetValue<double>("OpenTelemetry:SamplingRatio", 1.0);

            if (!enabled)
            {
                return builder;
            }

            builder.Services.AddOpenTelemetry()
                .ConfigureResource(r =>
                {
                    r.AddService(
                        serviceName: "Portal",
                        serviceVersion: serviceVersion);
                })
                .WithTracing(tracing =>
                {
                    if (environment.IsDevelopment())
                    {
                        tracing.SetSampler(new AlwaysOnSampler());
                    }
                    else
                    {
                        // Use sampling in production to reduce data volume
                        tracing.SetSampler(new TraceIdRatioBasedSampler(samplingRatio));
                    }

                    tracing
                        .AddAspNetCoreInstrumentation(opts =>
                        {
                            opts.RecordException = true;
                            opts.EnrichWithHttpRequest = (activity, request) =>
                            {
                                activity.SetTag("http.client_ip", request.HttpContext.Connection.RemoteIpAddress);
                            };
                            opts.EnrichWithHttpResponse = (activity, response) =>
                            {
                                activity.SetTag("http.response_content_type", response.ContentType);
                            };
                        })
                        .AddHttpClientInstrumentation(opts =>
                        {
                            opts.RecordException = true;
                            opts.FilterHttpRequestMessage = (message) =>
                            {
                                // Avoid tracing health check requests
                                return message.RequestUri?.AbsolutePath?.Contains("health") != true;
                            };
                        })
                        .AddEntityFrameworkCoreInstrumentation()
                        .AddSqlClientInstrumentation(opts =>
                        {
                            //opts.SetDbStatementForText = true;
                            opts.RecordException = true;
                        })
                        .AddSource("Portal.Services.Observability.PortalActivitySource");

                    // Console exporter for development visibility
                    if (environment.IsDevelopment() && consoleExporter)
                    {
                        tracing.AddConsoleExporter(opts =>
                        {
                            opts.Targets = ConsoleExporterOutputTargets.Console;
                        });
                    }

                    // OTLP exporter for backend (Jaeger, Grafana Tempo, Datadog, etc.)
                    if (!string.IsNullOrEmpty(otlpEndpoint))
                    {
                        tracing.AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(otlpEndpoint);
                            otlpOptions.Protocol = OtlpExportProtocol.Grpc;
                        });
                    }
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation();

                    // Console exporter for development
                    if (environment.IsDevelopment() && consoleExporter)
                    {
                        metrics.AddConsoleExporter();
                    }

                    // OTLP exporter for production
                    if (!string.IsNullOrEmpty(otlpEndpoint))
                    {
                        metrics.AddOtlpExporter(otlpOptions =>
                        {
                            otlpOptions.Endpoint = new Uri(otlpEndpoint);
                            otlpOptions.Protocol = OtlpExportProtocol.Grpc;
                        });
                    }
                });

            return builder;
        }
    }
}
