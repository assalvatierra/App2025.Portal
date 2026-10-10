# Outbox Cleanup Guide

## Overview

The `OutboxCleanupService` is a background worker that automatically cleans up successfully processed outbox messages to reclaim database storage. It runs daily and deletes messages older than a configurable retention period.

## Behavior

### What Gets Cleaned Up

Only messages with the following conditions are deleted:
- **Status**: `Processed` (value = 2)
- **ProcessedAt**: Not null and older than the retention cutoff date
- **Age**: Older than `RetentionDays` configuration (measured from `ProcessedAt`)

### What Is Preserved

The following messages are **never** deleted by cleanup:
- `Status = Pending` (not yet published)
- `Status = Failed` (awaiting retry)
- `Status = DeadLetter` (max retries exceeded; retained indefinitely for audit)

### Retention Policy

Processed messages are retained for a **configurable period** (default: 30 days) to support:
- Audit trails
- Replay/investigation of recent transactions
- Correlation with consumer-side idempotency records

Dead-letter messages are **retained indefinitely** to preserve evidence of publication failures.

## Configuration

### In `appsettings.json`

```json
{
  "MessageBroker": {
	"OutboxCleanup": {
	  "EnableCleanup": true,
	  "RetentionDays": 30
	}
  }
}
```

#### Options

- **EnableCleanup** (bool, default: `true`)
  - Set to `false` to disable cleanup at runtime without unregistering the service
  - Useful for debugging or during maintenance windows

- **RetentionDays** (int, default: `30`)
  - Number of days to retain successfully processed messages
  - Cleanup deletes messages where `ProcessedAt < DateTime.UtcNow.AddDays(-RetentionDays)`
  - Consider your audit/compliance requirements when setting this value
  - Suggested values:
	- `7` - High-volume event systems with short audit windows
	- `30` - Most production systems (default)
	- `90` - Regulatory/compliance-sensitive environments

### In `Program.cs`

```csharp
// Configure OutboxCleanupSettings from appsettings
builder.Services.Configure<Portal.Services.MessageBroker.OutboxCleanupSettings>(
	builder.Configuration.GetSection("MessageBroker:OutboxCleanup"));

// Register the background service
builder.Services.AddHostedService<Portal.Services.MessageBroker.OutboxCleanupService>();
```

> No manual registration is needed—the service is registered in the standard project setup.

## Operational Details

### Timing

- **Startup**: Service waits 30 seconds after application start before first cleanup run
- **Interval**: Runs every 24 hours thereafter (configurable via code change)
- **Batch Size**: Deletes up to 1,000 rows per batch to minimize lock contention
- **Cancellation**: Respects `CancellationToken` for graceful shutdown

### Logging

Cleanup operations are logged with the category `Portal.Services.MessageBroker.OutboxCleanupService`:

```
[Information] Outbox cleanup service started with 30 day(s) retention
[Debug] Deleted 150 processed outbox messages
[Debug] Deleted 200 processed outbox messages
[Information] Outbox cleanup completed: deleted 350 processed message(s)
```

- First startup logs the configured retention period
- Each batch size log (Debug level) helps monitor progress for large cleanup operations
- Final summary logs total rows deleted (Information level, only if > 0)
- Errors are logged at Error level with full exception details

### Error Handling

If cleanup fails:
1. The error is logged with full exception details
2. The service does **not** crash; it continues running
3. Cleanup is retried the next day
4. The service can be manually restarted by the application host

## Common Scenarios

### Query: How many processed outbox messages will be deleted?

```sql
DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -30, GETUTCDATE());

SELECT COUNT(*) AS ProcessedMessageCount
FROM dbo.OutboxMessage
WHERE [Status] = 2 -- Processed
  AND ProcessedAt IS NOT NULL
  AND ProcessedAt < @CutoffDate;
```

### Query: Show processed messages by age

```sql
SELECT 
	CASE 
		WHEN DATEDIFF(DAY, ProcessedAt, GETUTCDATE()) <= 7 THEN '0-7 days'
		WHEN DATEDIFF(DAY, ProcessedAt, GETUTCDATE()) <= 30 THEN '8-30 days'
		WHEN DATEDIFF(DAY, ProcessedAt, GETUTCDATE()) <= 90 THEN '31-90 days'
		ELSE '90+ days'
	END AS AgeRange,
	COUNT(*) AS Count
FROM dbo.OutboxMessage
WHERE [Status] = 2 -- Processed
  AND ProcessedAt IS NOT NULL
GROUP BY 
	CASE 
		WHEN DATEDIFF(DAY, ProcessedAt, GETUTCDATE()) <= 7 THEN '0-7 days'
		WHEN DATEDIFF(DAY, ProcessedAt, GETUTCDATE()) <= 30 THEN '8-30 days'
		WHEN DATEDIFF(DAY, ProcessedAt, GETUTCDATE()) <= 90 THEN '31-90 days'
		ELSE '90+ days'
	END
ORDER BY AgeRange;
```

### Query: Dead-letter messages (never cleaned up)

```sql
SELECT Id, MessageType, CreatedAt, ProcessedAt, Exception
FROM dbo.OutboxMessage
WHERE [Status] = 3 -- DeadLetter
ORDER BY ProcessedAt DESC;
```

### Scenario: Disable cleanup temporarily

To suspend cleanup without restarting the application:

```json
{
  "MessageBroker": {
	"OutboxCleanup": {
	  "EnableCleanup": false,
	  "RetentionDays": 30
	}
  }
}
```

Then reload configuration (depends on your hosting setup). Re-enable by changing `EnableCleanup` back to `true`.

### Scenario: Change retention period dynamically

If you need to increase retention for investigation:

1. Update `appsettings.json`:
   ```json
   {
	 "MessageBroker": {
	   "OutboxCleanup": {
		 "EnableCleanup": true,
		 "RetentionDays": 90
	   }
	 }
   }
   ```
2. Reload configuration (application restart or runtime configuration provider)
3. Next cleanup run will use the new retention period

### Scenario: Manual cleanup for urgent storage reclamation

If you need to purge old processed messages immediately:

```sql
-- Clean up messages older than 30 days
DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -30, GETUTCDATE());

DELETE FROM dbo.OutboxMessage
WHERE [Status] = 2 -- Processed only
  AND ProcessedAt IS NOT NULL
  AND ProcessedAt < @CutoffDate;
```

> **⚠️ Warning**: This runs synchronously and may lock the table. Test in non-production first.

## Monitoring & Alerts

### Check if cleanup is running

Query recent cleanup logs:

```csharp
// In your logging infrastructure, filter for:
Logger = "Portal.Services.MessageBroker.OutboxCleanupService"
```

### Alert on cleanup failures

Missing cleanup logs for 48+ hours may indicate:
- Disabled cleanup (`EnableCleanup = false`)
- Application not running
- Unhandled exception in cleanup service

Monitor application error logs for `OutboxCleanupService` errors.

### Storage impact

To estimate storage freed by a cleanup run:

```sql
SELECT 
	COUNT(*) AS RowsToDelete,
	(COUNT(*) * 2000) / 1024.0 AS EstimatedMBFreed -- Rough estimate: ~2KB per row
FROM dbo.OutboxMessage
WHERE [Status] = 2 -- Processed
  AND ProcessedAt IS NOT NULL
  AND ProcessedAt < DATEADD(DAY, -30, GETUTCDATE());
```

## See Also

- [implementation.md](./implementation.md) - Outbox pattern architecture
- [DeadLetterHandling.md](./DeadLetterHandling.md) - Dead-letter message retention
- [Idempotency.md](./Idempotency.md) - Idempotency record cleanup (similar pattern)
- [reference.md](./reference.md) - OutboxCleanupService API reference
