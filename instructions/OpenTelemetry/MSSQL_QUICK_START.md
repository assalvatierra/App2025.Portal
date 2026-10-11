# OpenTelemetry MSSQL Instrumentation - Quick Start

## What Was Added

### 1. **NuGet Package**
```xml
<PackageReference Include="OpenTelemetry.Instrumentation.SqlClient" Version="1.14.0-beta.1" />
```

### 2. **SqlClient Instrumentation Registration**
In `Services/Observability/OpenTelemetrySetup.cs`:
```csharp
.AddSqlClientInstrumentation(opts =>
{
	opts.RecordException = true;
})
```

### 3. **Enhanced PortalActivitySource**
Added MSSQL-specific attribute constants in `Services/Observability/PortalActivitySource.cs`:
- `MssqlServer` - Server instance identifier
- `MssqlDatabase` - Database name
- `MssqlCommandType` - Type of command (Text, StoredProcedure, etc.)
- `MssqlDurationMs` - Execution duration in milliseconds

## How It Works

All SQL Server queries executed through your application are now automatically traced:

```
HTTP Request (traced by AspNetCore instrumentation)
├── EF Core DbContext operations (traced by EntityFrameworkCore instrumentation)
│   └── SQL Server queries (traced by SqlClient instrumentation) ← NEW!
└── Direct SqlClient operations (traced by SqlClient instrumentation) ← NEW!
```

## Key Features

✅ **Automatic Tracing** - All SqlCommand executions captured without code changes  
✅ **Exception Handling** - Failed queries recorded with error details  
✅ **Query Text** - Full SQL statements visible in traces  
✅ **Correlation** - Traces linked to HTTP requests and EF Core operations  
✅ **Configurable** - Enable/disable via appsettings  
✅ **Production-Ready** - OTLP export to Jaeger, Grafana Tempo, Datadog, etc.  

## Viewing Traces

### Development (Console Output)
With `ConsoleExporter: true` in `appsettings.Development.json`, traces appear in debug output showing:
- SQL query text
- Database name
- Execution duration
- Any exceptions

### Production (OTLP Collector)
Configure `OtlpEndpoint` in `appsettings.json` to send traces to:
- **Jaeger** (`http://localhost:16686`)
- **Grafana Tempo** (integrated with Loki)
- **Datadog** (APM platform)
- **Azure Application Insights**
- Any OTLP-compatible backend

## Next Steps

1. **Test Locally**
   - Run your application in Development
   - Check Console output for SQL traces
   - Verify queries show up with execution times

2. **For Production**
   - Set `OtlpEndpoint` environment variable pointing to your collector
   - Adjust `SamplingRatio` based on query volume
   - Monitor trace ingestion in your observability platform

3. **Troubleshoot**
   - See `instructions/OpenTelemetry/MSSQL_INSTRUMENTATION.md` for detailed guide
   - Check that `OpenTelemetry:Enabled` is `true`
   - Verify your OTLP endpoint is accessible

## Custom SQL Tracing (Optional)

For operations outside EF Core, manually instrument:
```csharp
using Portal.Services.Observability;

var activity = PortalActivitySource.Instance.StartActivity("custom.sql.operation");
if (activity is not null)
{
	activity.SetTag("db.mssql.database", "MyDatabase");
	// Your code here
}
```

## Performance Impact

- **Minimal overhead** - Instrumentation is optimized for production use
- **Sampling** - Controls trace volume (default: 10% in production)
- **Connection pooling** - Fully compatible, no changes needed
- **Batch export** - OTLP exporter batches traces for efficiency

---
**Documentation:** See `instructions/OpenTelemetry/MSSQL_INSTRUMENTATION.md` for comprehensive guide
