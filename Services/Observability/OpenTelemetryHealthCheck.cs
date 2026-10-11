using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Portal.Services.Observability
{
    /// <summary>
    /// Health check for OpenTelemetry OTLP endpoint connectivity.
    /// Used to verify that the OTLP exporter can reach its configured endpoint during startup.
    /// </summary>
    public class OpenTelemetryHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OpenTelemetryHealthCheck> _logger;

        public OpenTelemetryHealthCheck(IConfiguration configuration, ILogger<OpenTelemetryHealthCheck> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var enabled = _configuration.GetValue<bool>("OpenTelemetry:Enabled");
                if (!enabled)
                {
                    _logger.LogInformation("OpenTelemetry is disabled, skipping health check");
                    return HealthCheckResult.Healthy("OpenTelemetry is disabled");
                }

                var otlpEndpoint = _configuration["OpenTelemetry:OtlpEndpoint"];
                if (string.IsNullOrEmpty(otlpEndpoint))
                {
                    _logger.LogWarning("OpenTelemetry OTLP endpoint not configured");
                    return HealthCheckResult.Degraded("OTLP endpoint not configured, using Console exporter only");
                }

                // Parse the URI to extract host and port
                var uri = new Uri(otlpEndpoint);
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : (uri.Scheme == "https" ? 443 : 80);

                // Attempt to connect to the OTLP endpoint
                _logger.LogInformation("Checking OpenTelemetry OTLP endpoint connectivity to {Host}:{Port}", host, port);

                // Try to establish a TCP connection to verify the endpoint is reachable
                using var socket = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Stream,
                    ProtocolType.Tcp);

                try
                {
                    var connectTask = socket.ConnectAsync(host, port);
                    var completedTask = await Task.WhenAny(
                        connectTask,
                        Task.Delay(5000, cancellationToken));

                    if (completedTask != connectTask)
                    {
                        socket.Close();
                        _logger.LogWarning(
                            "OpenTelemetry OTLP endpoint at {Endpoint} connection timed out. " +
                            "Application will continue with Console exporter only.",
                            otlpEndpoint);

                        return HealthCheckResult.Degraded(
                            $"OTLP endpoint connection timed out, using Console exporter only");
                    }

                    socket.Close();

                    _logger.LogInformation("OpenTelemetry OTLP endpoint is reachable at {Endpoint}", otlpEndpoint);
                    return HealthCheckResult.Healthy($"OTLP endpoint reachable at {otlpEndpoint}");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning(
                        "OpenTelemetry OTLP endpoint check was cancelled. " +
                        "Application will continue with Console exporter only.");
                    return HealthCheckResult.Degraded("OTLP endpoint check cancelled, using Console exporter only");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "OpenTelemetry OTLP endpoint at {Endpoint} is unreachable. " +
                        "Application will continue with Console exporter only. Error: {Error}",
                        otlpEndpoint,
                        ex.Message);

                    return HealthCheckResult.Degraded(
                        $"OTLP endpoint unreachable, using Console exporter only");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during OpenTelemetry health check");
                return HealthCheckResult.Unhealthy("OpenTelemetry health check failed", ex);
            }
        }
    }
}
