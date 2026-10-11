# Complete OpenTelemetry Architecture

## Instrumentation Hierarchy

```
┌────────────────────────────────────────────────────────────────┐
│                     HTTP Request (Browser)                     │
└────────────────────────────────────────────────────────────────┘
							  │
							  ▼
┌────────────────────────────────────────────────────────────────┐
│          ASP.NET Core Instrumentation (Traced)                 │
│  ├─ Request path, method, status code                          │
│  ├─ Request/response headers & body size                       │
│  ├─ Client IP address (enriched)                               │
│  ├─ Response content type (enriched)                           │
│  └─ Exceptions with stack traces                               │
└────────────────────────────────────────────────────────────────┘
							  │
				┌─────────────┼─────────────┐
				▼             ▼             ▼
	 ┌──────────────────┐  ┌──────┐  ┌─────────────┐
	 │ Entity Framework │  │Cache │  │HTTP Outbound│
	 │ (Traced)         │  │Ops   │  │Calls(Traced)│
	 │ ├─SaveChanges    │  │      │  │             │
	 │ ├─Query          │  │      │  │             │
	 │ └─Delete         │  │      │  │             │
	 └────────┬─────────┘  └──────┘  └─────────────┘
			  │
			  ▼
	 ┌────────────────────────────────────────┐
	 │  SQL Client Instrumentation (Traced)   │  ← NEW!
	 │  ├─ SqlCommand execution                │
	 │  ├─ Query text (SetDbStatementForText) │
	 │  ├─ Database name                      │
	 │  ├─ Command type                       │
	 │  ├─ Execution duration                 │
	 │  └─ Exceptions with details            │
	 └────────────┬───────────────────────────┘
				  │
				  ▼
	 ┌────────────────────────────────────────┐
	 │    SQL Server Instance                 │
	 │    ├─ YourDatabase                     │
	 │    │  ├─ dbo.Users                     │
	 │    │  ├─ dbo.Orders                    │
	 │    │  └─ dbo.Products                  │
	 │    └─ System databases                 │
	 └────────────────────────────────────────┘
```

## Message Handler Flow (Event-Driven)

```
┌─────────────────────────────────────────────────┐
│   Message Published (Outbox Pattern)            │
│   └─ MessageConsumerBase Activity (Traced)      │
└────────────────┬────────────────────────────────┘
				 │
				 ▼
┌─────────────────────────────────────────────────┐
│   Message Ingested                              │
│   └─ Activity: message.consume (Traced)         │
│      ├─ Message ID                              │
│      ├─ Message Type                            │
│      ├─ Partition Key                           │
│      └─ Processing Status                       │
└────────────────┬────────────────────────────────┘
				 │
				 ▼
┌─────────────────────────────────────────────────┐
│   Handler Processing                            │
│   ├─ EF Core operations (Traced)                │
│   ├─ SQL Client operations (Traced) ← NEW!      │
│   └─ Custom activities (if instrumented)        │
└────────────────┬────────────────────────────────┘
				 │
				 ▼
┌─────────────────────────────────────────────────┐
│   Message Completion                            │
│   └─ Activity Status: Success/Failed/Skipped    │
└─────────────────────────────────────────────────┘
```

## Data Collection & Export Flow

```
┌──────────────────────────────────────────────────┐
│              Application Running                 │
│  ├─ AspNetCore Instrumentation                  │
│  ├─ Http Instrumentation                        │
│  ├─ EntityFramework Instrumentation             │
│  ├─ SqlClient Instrumentation ← NEW!            │
│  └─ Custom Activities                           │
└──────────────────┬───────────────────────────────┘
				   │
		┌──────────┴──────────┐
		│                     │
		▼                     ▼
  ┌──────────────┐      ┌──────────────┐
  │Development   │      │Production    │
  ├──────────────┤      ├──────────────┤
  │Console Export│      │OTLP Exporter │
  │(Console Out) │      │(gRPC)        │
  └──────┬───────┘      └──────┬───────┘
		 │                     │
		 ▼                     ▼
  ┌──────────────┐      ┌──────────────────────┐
  │Console Output│      │OTLP Collector        │
  │(Real-time)   │      │ ├─ Jaeger           │
  └──────────────┘      │ ├─ Grafana Tempo    │
						│ ├─ Datadog          │
						│ ├─ Azure App Insights
						│ └─ Other OTLP-compat
						└──────┬───────────────┘
							   │
							   ▼
						┌──────────────────┐
						│Visualization     │
						├─ Trace Explorer  │
						├─ Service Graph   │
						├─ Latency Analysis
						└─ Error Tracking  │
						└──────────────────┘
```

## Trace Context Propagation

```
┌──────────────────────────────────────────────┐
│ HTTP Request Headers (Automatic)             │
│ ├─ traceparent: 00-[TraceId]-[SpanId]-01     │
│ └─ tracestate: vendor-specific-state         │
└──────────────────┬───────────────────────────┘
				   │
				   ▼
┌──────────────────────────────────────────────┐
│ All Child Operations Inherit TraceId         │
│ ├─ EF Core queries                          │
│ ├─ SqlClient commands ← NEW!                │
│ ├─ HTTP outbound calls                      │
│ ├─ Message consumption                      │
│ └─ Custom activities                        │
└──────────────────┬───────────────────────────┘
				   │
				   ▼
┌──────────────────────────────────────────────┐
│ Serilog Logs Enriched                        │
│ ├─ TraceId in all log entries                │
│ ├─ SpanId in structured logs                 │
│ ├─ Correlation across services               │
│ └─ Cross-platform trace linkage              │
└──────────────────────────────────────────────┘
```

## Attribute Mapping

### HTTP (AspNetCore)
```
http.method              → GET, POST, etc.
http.status_code         → 200, 404, 500
http.url                 → Full request URL
http.scheme              → https, http
http.client_ip           → Remote IP (enriched)
http.response_content_type   → Content-Type (enriched)
http.target              → URL path & query
```

### Database (EF Core + SqlClient)
```
db.system                → "mssql"
db.name                  → "YourDatabase"
db.user                  → "domain\username"
db.statement             → Full SQL query (if enabled)
db.operation             → SELECT, INSERT, UPDATE, DELETE
db.mssql.database        → Database name
db.mssql.command_type    → Text, StoredProcedure
db.mssql.server_instance → "server\instance"
db.connection.string     → (hidden for security)
exception.type           → SqlException, etc.
exception.message        → Error detail
```

### Messages (Custom)
```
messaging.message_id     → Unique message ID
messaging.message_type   → Payload type name
messaging.partition_key  → Partition for ordering
messaging.skipped_reason → Why message wasn't processed
messaging.dead_lettered  → Dead letter status
```

## Performance Characteristics

| Metric | Value | Notes |
|--------|-------|-------|
| **CPU Overhead** | 1-2% | Minimal instrumentation cost |
| **Memory Impact** | <10MB | Traces streamed, not buffered |
| **Latency Addition** | <1ms | Per operation |
| **Network Bandwidth** | Configurable | OTLP batching + compression |
| **Sampling Reduction** | 10% → 1/10th data | ~0.1% CPU impact with sampling |

## Configuration File Structure

```json
{
  "OpenTelemetry": {
	"Enabled": true,                    // Master switch
	"ConsoleExporter": true,            // Dev: console output
	"OtlpEndpoint": "http://...:4317",  // Production backend
	"SamplingRatio": 1.0                // 100% in dev, 10% in prod
  }
}
```

## Integration Points

### Your Application
- `Program.cs` - Startup initialization
- `appsettings.json` - Configuration
- `MessageConsumerBase.cs` - Message tracing
- `PortalActivitySource.cs` - Custom activities

### OpenTelemetry Libraries
- `OpenTelemetry` - Core framework
- `OpenTelemetry.Instrumentation.AspNetCore` - HTTP tracing
- `OpenTelemetry.Instrumentation.Http` - Outbound calls
- `OpenTelemetry.Instrumentation.EntityFrameworkCore` - EF Core
- `OpenTelemetry.Instrumentation.SqlClient` - SQL Server ← NEW!
- `OpenTelemetry.Exporter.Console` - Console output
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` - OTLP export

### External Services
- **Serilog** - Structured logging (integrated)
- **OTLP Collector** - Trace aggregation (production)
- **Visualization** - Jaeger, Grafana, Datadog, etc.
- **Alerting** - Based on trace data

## Security Considerations

✅ **Query Masking**
- Use parameterized queries (@parameter syntax)
- Connection strings hidden in traces
- Sensitive data excluded by default

✅ **Access Control**
- OTLP endpoint should be behind auth in production
- Only authorized services access trace data
- Separate read-only access for analysis

✅ **Data Retention**
- Configure retention policies
- Compliance with data protection regulations
- Automatic trace cleanup

## Next Steps

1. **Verify Setup**
   ```bash
   dotnet build  # Should succeed ✓
   ```

2. **Test Locally**
   ```bash
   # Run with Debug environment
   # Make database queries
   # Check console for SQL traces
   ```

3. **Deploy to Production**
   - Set `OTEL_EXPORTER_OTLP_ENDPOINT` environment variable
   - Configure trace backend (Jaeger, etc.)
   - Monitor trace ingestion

---
See other files in `instructions/OpenTelemetry/` for detailed guides and troubleshooting.
