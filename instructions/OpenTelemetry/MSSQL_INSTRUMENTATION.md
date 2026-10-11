## MSSQL Instrumentation Implementation Guide

### Overview
Your Portal application now has comprehensive MSSQL instrumentation enabled through OpenTelemetry's SqlClient instrumentation. This tracks all SQL Server operations at the ADO.NET level.

### What's Instrumented

#### 1. **Automatic SqlClient Instrumentation**
   - All `SqlCommand` executions are traced
   - Includes query text (when `RecordException = true`)
   - Exception recording for failed queries
   - Automatic correlation with OpenTelemetry context (TraceId/SpanId)

#### 2. **Entity Framework Core Integration**
   - EF Core operations are traced at the LINQ/DbContext level
   - Combined with SqlClient tracing provides end-to-end visibility
   - Both high-level EF operations and low-level SQL execution are captured

#### 3. **Standard Database Attributes Captured**
   - `db.system`: "mssql"
   - `db.name`: Database name
   - `db.user`: Connection user
   - `db.statement`: SQL query text (when enabled)
   - `db.operation`: Type of operation (SELECT, INSERT, UPDATE, DELETE)
   - Exception details if query fails

### Tracing Your Custom MSSQL Operations

For custom database operations outside of EF Core, use the PortalActivitySource:

```csharp
using Portal.Services.Observability;
using System.Diagnostics;

// In your service class:
var activity = PortalActivitySource.Instance.StartActivity(
	PortalActivitySource.ActivityNames.RepositoryQuery,
	ActivityKind.Client);

if (activity is not null)
{
	activity.SetTag(PortalActivitySource.AttributeNames.DatabaseName, "YourDatabase");
	activity.SetTag(PortalActivitySource.AttributeNames.DatabaseOperation, "CustomQuery");
	activity.SetTag(PortalActivitySource.AttributeNames.MssqlDatabase, "YourDatabase");
	activity.SetTag(PortalActivitySource.AttributeNames.MssqlCommandType, "Text");

	try
	{
		// Your MSSQL operation here
		using var command = connection.CreateCommand();
		command.CommandText = "SELECT * FROM YourTable WHERE Id = @id";
		command.Parameters.AddWithValue("@id", id);

		var startTime = DateTime.UtcNow;
		var result = await command.ExecuteReaderAsync();
		var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;

		activity.SetTag(PortalActivitySource.AttributeNames.MssqlDurationMs, duration);
		return result;
	}
	catch (SqlException ex)
	{
		activity.SetStatus(ActivityStatusCode.Error, ex.Message);
		throw;
	}
}
```

### Configuration

#### Development Environment (`appsettings.Development.json`)
- **ConsoleExporter**: `true` - see traces in console output
- **SamplingRatio**: `1.0` - capture all traces
- Ideal for debugging SQL performance issues locally

#### Production Environment (`appsettings.json`)
- **ConsoleExporter**: `false` - disabled for performance
- **OtlpEndpoint**: Points to your observability backend
- **SamplingRatio**: `0.1` (10%) - reduces data volume while maintaining visibility
- Adjust sampling based on your query volume

### Viewing MSSQL Traces

#### In Console (Development)
When `ConsoleExporter` is enabled in development, you'll see output like:
```
Activity.TraceId:            12345678901234567890123456789012
Activity.SpanId:             1234567890123456
Activity.DisplayName:        sql
Activity.Kind:               Client
Activity.StartTime:          2024-01-15T10:30:45.1234567Z
Activity.Duration:           00:00:00.0542310
Activity.Tags:
	db.system: mssql
	db.statement: SELECT * FROM Users WHERE Id = @p0
	db.name: YourDatabase
```

#### In OTLP Collector (Production)
Traces are exported to your configured OTLP endpoint (Jaeger, Grafana Tempo, etc.) where you can:
- View SQL query execution timings
- Correlate database operations with HTTP requests
- Identify slow queries
- Track query success/failure rates
- Map database operations to business transactions

### Performance Considerations

1. **Query Text Recording**: Enabled by default. Contains full SQL statements including data values. Be aware of:
   - Sensitive data in query parameters
   - Large query texts impacting trace size
   - To disable, set `RecordException = false` (trades detail for performance)

2. **Sampling**: In production, adjust `SamplingRatio`:
   - `1.0` (100%) - captures everything, higher overhead
   - `0.1` (10%) - recommended for high-traffic applications
   - `0.01` (1%) - for very high query volume services

3. **Connection Pooling**: SqlClient tracing doesn't interfere with connection pooling. All pooled connections are traced normally.

### Troubleshooting

**No MSSQL traces appearing?**
1. Verify `OpenTelemetry:Enabled` is `true` in appsettings
2. Check that `ConsoleExporter` is `true` in development
3. Ensure your queries are actually executing (check application logs)
4. Verify the OTLP endpoint is configured and reachable

**Too many traces in production?**
- Increase `SamplingRatio` (lower value = fewer traces)
- Disable `RecordException` if query texts aren't needed
- Configure OTLP exporter batching for better throughput

**Sensitive data in traces?**
- Avoid binding sensitive values to activity tags
- Use parameterized queries (built-in with SqlCommand)
- Set query sensitive settings in your OTLP backend to mask PII

### Best Practices

1. **Use parameterized queries** - Always prefer `@parameter` style over string concatenation
2. **Tag custom operations** - Add meaningful tags to your custom activities for better filtering
3. **Monitor sampling rate** - Ensure your sampling rate in production doesn't skip important errors
4. **Correlate with logs** - Use TraceId from traces to find corresponding log entries
5. **Set operation names** - Use descriptive `ActivityNames` for meaningful trace names

### Related Files
- `Services/Observability/OpenTelemetrySetup.cs` - MSSQL instrumentation configuration
- `Services/Observability/PortalActivitySource.cs` - MSSQL attribute definitions
- `appsettings.json` - OpenTelemetry configuration
- `appsettings.Development.json` - Development-specific overrides
