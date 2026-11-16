# Dead Letter Handling - Implementation Complete ✅

## Overview

Dead letter handling has been fully implemented for the Portal MessageBroker system. Messages that fail to publish after the maximum number of retries (default: 5) are now automatically moved to a "dead-letter" state with critical logging and optional email notifications.

---

## What Was Implemented

### 1. Status Tracking
- **File**: `Services/MessageBroker/OutboxMessageStatus.cs`
- **Enum values**: 
  - `Pending = 0` - Awaiting publication
  - `Failed = 1` - Last attempt failed; retrying
  - `Processed = 2` - Successfully published
  - `DeadLetter = 3` - Max retries exceeded; no further attempts

### 2. Core Functionality
- **OutboxMessage.cs** - Added `Status` property to track message lifecycle
- **OutboxService.cs** - Added `MarkAsDeadLetterAsync()` method, updated queries to filter by Status
- **OutboxPollingService.cs** - Detects when retries are exhausted and moves message to DeadLetter
- **Database** - New column, index, and SQL migration script

### 3. Notification Services

#### Basic Logging (`DeadLetterNotificationService`)
- Logs critical error with full message context
- No configuration needed
- Always-on regardless of email setup

#### Email Alerts (`DeadLetterEmailNotificationService`) ⭐ NEW
- Sends HTML-formatted emails to configured recipients
- Includes: Message ID, Type, CorrelationId, timestamps, error details, payload preview
- Configurable recipients (semicolon-separated list)
- Graceful fallback if email send fails
- **Already registered by default in Program.cs**

### 4. Database Schema
```sql
-- Added to OutboxMessage table
ALTER TABLE dbo.OutboxMessage ADD [Status] INT NOT NULL DEFAULT 0;

-- New composite index for efficient queries
CREATE NONCLUSTERED INDEX IX_OutboxMessage_Status_CreatedAt
ON dbo.OutboxMessage([Status], [CreatedAt]);
```

**Execute SQL script**: `instructions/MessageBroker/DeadLetterIndex.sql`

---

## Configuration

### 1. Email Recipients (Optional)

Update `appsettings.json`:

```json
{
  "MessageBroker": {
	"DeadLetterNotification": {
	  "EmailRecipients": "admin@example.com;support@example.com"
	}
  }
}
```

Multiple recipients supported (semicolon or comma-separated).

### 2. Email Settings

Ensure `EmailSettings` is configured (already required for existing email features):

```json
{
  "EmailSettings": {
	"Host": "smtp.gmail.com",
	"Port": 587,
	"EnableSsl": true,
	"FromAddress": "noreply@example.com",
	"FromName": "Portal Application",
	"UserName": "your-email@gmail.com",
	"Password": "your-app-password"
  }
}
```

### 3. DI Registration

Already configured in `Program.cs`:

```csharp
// Sends email alerts (default)
builder.Services.AddScoped<IDeadLetterNotificationService, DeadLetterEmailNotificationService>();

// Or use logging-only:
// builder.Services.AddScoped<IDeadLetterNotificationService, DeadLetterNotificationService>();
```

---

## Behavior Flow

```
Message Published
	↓
Polling Service Attempts to Publish
	↓
	├─ SUCCESS → Status = Processed ✅
	│
	└─ FAILURE (Exception)
		 ↓
		 Increment RetryCount
		 Set Status = Failed
		 ↓
		 ├─ RetryCount < MaxRetries
		 │  └─ Next polling cycle: retry
		 │
		 └─ RetryCount >= MaxRetries (5)
			├─ Set Status = DeadLetter ⚠️
			├─ Set ProcessedAt = now
			├─ Log critical event
			└─ Send email notification (if configured)
				└─ Includes message context and error details
```

---

## File Changes Summary

### New Files Created
1. `Services/MessageBroker/OutboxMessageStatus.cs` - Status enum
2. `Services/MessageBroker/IDeadLetterNotificationService.cs` - Notification interface
3. `Services/MessageBroker/DeadLetterNotificationService.cs` - Logging-only implementation
4. `Services/MessageBroker/DeadLetterEmailNotificationService.cs` - Email implementation ⭐
5. `instructions/MessageBroker/DeadLetterHandling.md` - Complete behavior documentation
6. `instructions/MessageBroker/DeadLetterEmailNotification.md` - Email setup guide
7. `instructions/MessageBroker/DEADLETTER_EMAIL_SETUP.md` - Quick-start guide
8. `instructions/MessageBroker/DeadLetterIndex.sql` - Database migration script
9. `instructions/MessageBroker/README.md` - Documentation index and quick reference

### Modified Files
1. `Services/MessageBroker/OutboxMessage.cs` - Added `Status` property
2. `Services/MessageBroker/IOutboxService.cs` - Added `MarkAsDeadLetterAsync()`, updated return types
3. `Services/MessageBroker/OutboxService.cs` - Implemented dead-letter logic, status filters
4. `Services/MessageBroker/OutboxPollingService.cs` - Detects max retries, triggers notifications
5. `Data/ApplicationDbContext.cs` - Added Status configuration and index
6. `Program.cs` - Registered `DeadLetterEmailNotificationService`
7. `appsettings.json` - Added `MessageBroker:DeadLetterNotification` config
8. `instructions/MessageBroker/implementation.md` - Updated checklist and documentation

---

## Testing Dead-Letter Scenarios

### Quick Test (SQL Insert)
```sql
INSERT INTO dbo.OutboxMessage 
(Id, MessageType, Payload, CreatedAt, RetryCount, Status, Exception, ProcessedAt)
VALUES
(NEWID(), 'TestDeadLetter', '{"test":"data"}', GETUTCDATE(), 5, 3, 'Test error', GETUTCDATE());
```

Then check:
- **Logs**: Should see critical entries
- **Email**: Should receive notification (if configured)
- **Database**: Status = 3 (DeadLetter)

### Integration Test
1. Create `TestDeadLetterConsumer : IMessageConsumer` that throws
2. Publish a test message matching that type
3. Wait for polling cycles (5 retries × ~15s = ~75s)
4. Verify: OutboxMessage.Status = 3
5. Verify: Critical log entries
6. Verify: Email notification (if configured)

See [DeadLetterHandling.md](../MessageBroker/DeadLetterHandling.md) for more testing scenarios.

---

## Emails Content

When a message is dead-lettered, recipients receive an HTML email with:

- **Header**: 🚨 Outbox Message Dead Letter Alert
- **Message Details**:
  - Message ID (unique identifier)
  - Message Type (event category)
  - Correlation ID (transaction tracing)
  - Created At (timestamp)
  - Retry Count (how many attempts were made)
- **Last Error**: Full exception details
- **Payload Preview**: First 500 characters of message content
- **Action Items**: Guidance for investigation

Example subject: `🚨 Dead Letter Alert: Message 12345678-1234-1234-1234-123456789012`

---

## Querying Dead-Lettered Messages

### Find Recent Dead Letters
```sql
SELECT TOP 10 
	Id,
	MessageType,
	CorrelationId,
	CreatedAt,
	ProcessedAt,
	RetryCount,
	Exception
FROM dbo.OutboxMessage
WHERE Status = 3  -- DeadLetter
ORDER BY ProcessedAt DESC;
```

### Track a Message End-to-End
```sql
DECLARE @correlationId UNIQUEIDENTIFIER = 'your-id-here';

-- Find outbox message
SELECT * FROM dbo.OutboxMessage WHERE CorrelationId = @correlationId;

-- Find processed messages (idempotency records)
SELECT * FROM dbo.ProcessedMessage 
WHERE MessageId IN (
	SELECT Id FROM dbo.OutboxMessage WHERE CorrelationId = @correlationId
);
```

### Find Messages Stuck in Retry Loop
```sql
SELECT 
	Id,
	MessageType,
	CorrelationId,
	RetryCount,
	Exception
FROM dbo.OutboxMessage
WHERE Status = 1  -- Failed (not dead-lettered yet)
  AND RetryCount >= 4
ORDER BY CreatedAt ASC;
```

---

## Next Steps

### Immediate (Next Sprint)
1. **Apply SQL migration** - Run `DeadLetterIndex.sql` against your database
2. **Update appsettings.json** - Add email recipients
3. **Test email notifications** - Trigger a dead-letter scenario to verify emails work
4. **Monitor logs** - Verify critical events appear in Serilog/Seq

### Short Term (This Quarter)
1. **Add monitoring/alerting** - Alert ops when dead-letter queue grows
2. **Create admin UI** - Dashboard to view and manage dead-lettered messages
3. **Implement manual retry** - Allow ops to republish dead-lettered messages
4. **Archive old records** - Add cleanup job for dead-lettered messages after N days (if desired)

### Long Term (Next Quarter)
1. **Replace in-memory transport** - Switch from MassTransit in-memory to RabbitMQ or Azure Service Bus
2. **Add metrics collection** - Track Published, Processed, Failed, DeadLetter counts
3. **Implement distributed locking** - Support multiple app instances scalably
4. **Enhance notifications** - Add Slack, PagerDuty, or other alerting integrations

---

## Troubleshooting

### Emails Not Sending?
- Verify `MessageBroker:DeadLetterNotification:EmailRecipients` is set in appsettings
- Verify `EmailSettings` (Host, Port, Username, Password) is correct
- Test email service independently
- Check logs for email send errors (search for "Failed to send dead-letter")

### Messages Not Being Dead-Lettered?
- Check `OutboxMessage` table: Is Status being set to 3?
- Verify `OutboxPollingService` is running (check logs)
- Check MassTransit configuration
- Verify max retries setting (default 5)

### Too Many Dead-Letters?
- Investigate root cause (service down? data validation failure?)
- Fix underlying issue
- Republish messages manually (update Status = 0, RetryCount = 0)
- Add alerting to get notified sooner

See [DeadLetterHandling.md](../MessageBroker/DeadLetterHandling.md) for more troubleshooting.

---

## Documentation

### Essential Reading
- **[README.md](../MessageBroker/README.md)** - Complete documentation index and quick reference ⭐ START HERE
- **[implementation.md](../MessageBroker/implementation.md)** - Architectural overview and design patterns
- **[DeadLetterHandling.md](../MessageBroker/DeadLetterHandling.md)** - Dead-letter behavior and operations
- **[DeadLetterEmailNotification.md](../MessageBroker/DeadLetterEmailNotification.md)** - Email configuration and extension

### Quick Reference
- **[reference.md](../MessageBroker/reference.md)** - API reference and code examples
- **[DEADLETTER_EMAIL_SETUP.md](../MessageBroker/DEADLETTER_EMAIL_SETUP.md)** - 3-step email setup
- **[Idempotency.md](../MessageBroker/Idempotency.md)** - Duplicate prevention details

---

## Build Status

✅ **Build Successful** - All code compiles without errors

```
Build successful
Total Time: 0:00:XX
```

---

## Implementation Checklist

- [x] Create OutboxMessageStatus enum
- [x] Add Status property to OutboxMessage entity
- [x] Implement IDeadLetterNotificationService interface
- [x] Create DeadLetterNotificationService (logging)
- [x] Create DeadLetterEmailNotificationService (email) ⭐
- [x] Update OutboxService with dead-letter methods
- [x] Update OutboxPollingService to detect and handle dead letters
- [x] Update ApplicationDbContext with Status configuration and index
- [x] Register services in Program.cs
- [x] Add configuration to appsettings.json
- [x] Create SQL migration script
- [x] Create comprehensive documentation
- [x] Verify build succeeds
- [ ] Apply SQL migration to database
- [ ] Test email notifications
- [ ] Add monitoring/alerting
- [ ] Document runbook for operations team

---

## Questions?

Refer to the **[README.md](../MessageBroker/README.md)** documentation index for quick answers to common questions like:
- "How do I test dead-letter scenarios?"
- "How do I query dead-lettered messages?"
- "How do I re-publish a dead-lettered message?"
- "How do I add custom notifications (Slack, webhook, etc.)?"

---

**Generated:** December 2024  
**Status**: ✅ Complete and Ready for Testing  
**Next Action**: Apply SQL migration and verify email notifications
