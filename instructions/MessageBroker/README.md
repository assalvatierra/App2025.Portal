# MessageBroker Documentation Index

This directory contains comprehensive documentation for the Portal MessageBroker system, which implements an event-driven architecture for asynchronous communication using MassTransit and database outbox patterns.

## Quick Links by Use Case

### I want to...

**...publish a message**
- See: [implementation.md](implementation.md) → "Outbox Pattern" section
- Code example: `IOutboxPublisher.PublishAsync<T>(payload)`
- Database: Messages are persisted to `OutboxMessage` table before publishing

**...handle an event (create a consumer)**
- See: [implementation.md](implementation.md) → "Consumer Abstraction" section
- Create a class: `class MyConsumer : MessageConsumerBase<MyEventPayload>`
- No manual registration needed (auto-discovered via `AddMessageConsumers()`)

**...investigate dead-lettered messages**
- See: [DeadLetterHandling.md](DeadLetterHandling.md)
- Query: `SELECT * FROM OutboxMessage WHERE Status = 3`
- Check logs for critical entries with MessageId and CorrelationId

**...enable email alerts for dead-letter messages**
- See: [DeadLetterEmailNotification.md](DeadLetterEmailNotification.md)
- Setup: Configure `MessageBroker:DeadLetterNotification:EmailRecipients`
- Implementation: `DeadLetterEmailNotificationService` (already registered)

**...prevent duplicate message processing**
- See: [Idempotency.md](Idempotency.md)
- Already implemented; enabled by default
- Per-message-per-consumer duplicate detection using `ProcessedMessage` table

**...test dead-letter scenarios**
- See: [DeadLetterHandling.md](DeadLetterHandling.md) → "Detecting and Handling" section
- Use: Create test `TestDeadLetterConsumer` that always throws
- Or: Manually insert test message with Status=3 (DeadLetter)
- View: Check `OutboxMessage` table, logs, and email inbox

---

## Documentation Files

### Core Documentation

#### [implementation.md](implementation.md) ⭐ START HERE
**Purpose:** Complete architectural overview and implementation details
- BrokerMessage contract and properties
- Outbox pattern flow and transaction semantics
- Idempotency implementation
- Consumer abstraction and lifecycle
- First event example: ReservationVerified
- Status checklist of completed/pending features

**Key Sections:**
- Outbox (3.1) - Message persistence and polling
- Idempotency (3.2) - Duplicate prevention
- Consumer Abstraction (3.3) - Event handling
- Dead Letter Handling - New! (3.4)

#### [reference.md](reference.md)
**Purpose:** API reference documentation
- BrokerMessage class definition and examples
- IOutboxPublisher interface and methods
- IMessageConsumer interface
- MessageConsumerBase<T> generic class
- IOutboxService interface (internal)
- Configuration and DI setup

**Best for:** Looking up specific class/method signatures

---

### Dead Letter Documentation

#### [DeadLetterHandling.md](DeadLetterHandling.md)
**Purpose:** Dead-letter behavior, querying, and basic operations
- Status enum: Pending (0), Failed (1), Processed (2), DeadLetter (3)
- Retry logic and max retry threshold
- Detecting dead-lettered messages via database queries
- Extending/customizing notification services
- Monitoring dead-letter queue

**Best for:** Understanding what happens when messages fail

#### [DeadLetterEmailNotification.md](DeadLetterEmailNotification.md)
**Purpose:** Email notification setup and customization
- Configuration options (appsettings.json)
- Email body content and formatting
- SMTP settings requirements
- Behavior and error handling
- Extension examples (webhooks, Slack, PagerDuty)
- Troubleshooting email send failures

**Best for:** Setting up email alerts or extending notifications

#### [DEADLETTER_EMAIL_SETUP.md](DEADLETTER_EMAIL_SETUP.md)
**Purpose:** Quick-start guide for email notifications
- 3-step setup checklist
- Example configuration values
- Behavior summary
- Rollback instructions

**Best for:** Quick implementation reference

---

### Supporting Documentation

#### [Idempotency.md](Idempotency.md)
**Purpose:** Duplicate message prevention
- Problem statement and solution
- ProcessedMessage table schema
- Per-consumer idempotency semantics
- Cleanup strategy and retention periods
- Troubleshooting duplicate messages
- Configuration options

**Best for:** Understanding how duplicate messages are prevented

---

## Database Schema

### OutboxMessage Table
```sql
CREATE TABLE dbo.OutboxMessage (
	Id UNIQUEIDENTIFIER PRIMARY KEY,
	MessageType NVARCHAR(256) NOT NULL,
	Payload NVARCHAR(MAX) NOT NULL,
	CorrelationId UNIQUEIDENTIFIER NULL,
	PartitionKey NVARCHAR(256) NULL,
	Headers NVARCHAR(MAX) NULL,
	CreatedAt DATETIME2 NOT NULL,
	ProcessedAt DATETIME2 NULL,
	RetryCount INT NOT NULL DEFAULT 0,
	Exception NVARCHAR(MAX) NULL,
	Status INT NOT NULL DEFAULT 0,  -- 0=Pending, 1=Failed, 2=Processed, 3=DeadLetter
	INDEX IX_OutboxMessage_Status_CreatedAt (Status, CreatedAt)
);
```

See [DeadLetterIndex.sql](DeadLetterIndex.sql) to add Status column and indexes.

### ProcessedMessage Table
```sql
CREATE TABLE dbo.ProcessedMessage (
	Id UNIQUEIDENTIFIER PRIMARY KEY,
	MessageId UNIQUEIDENTIFIER NOT NULL,
	ConsumerType NVARCHAR(256) NOT NULL,
	ProcessingStartedAt DATETIME2 NOT NULL,
	ProcessedAt DATETIME2 NULL,
	ExpiresAt DATETIME2 NOT NULL,
	UNIQUE (MessageId, ConsumerType),
	INDEX IX_ProcessedMessage_ExpiresAt (ExpiresAt)
);
```

See [CreateProcessedMessageTable.sql](CreateProcessedMessageTable.sql) for full DDL.

---

## Code Locations

### Core Message Broker Files
```
Services/MessageBroker/
├── BrokerMessage.cs                              ← Message contract
├── OutboxMessage.cs                              ← EF entity
├── OutboxMessageStatus.cs                        ← Status enum (NEW)
├── IOutboxService.cs                             ← Service interface
├── OutboxService.cs                              ← Service implementation
├── OutboxPublisher.cs                            ← Publisher implementation
├── OutboxPollingService.cs                       ← Background polling worker
├── IMessageConsumer.cs                           ← Consumer interface
├── MessageConsumerBase.cs                        ← Consumer base class
├── BrokerMessageConsumer.cs                      ← MassTransit consumer
├── MessageBrokerServiceExtensions.cs             ← DI extensions
├── IDeadLetterNotificationService.cs             ← Notification interface (NEW)
├── DeadLetterNotificationService.cs              ← Logging implementation (NEW)
└── DeadLetterEmailNotificationService.cs         ← Email implementation (NEW)
```

### Consumers
```
Services/Consumers/
├── ReservationVerifiedConsumer.cs                ← Example consumer (logs)
├── ReservationVerifiedConsumerEmailSender.cs    ← Example consumer (sends email)
└── ReservationVerifiedInternalEmailSender.cs   ← Example consumer (internal notify)
```

### Configuration
```
Program.cs                              ← MassTransit, Outbox, Idempotency, DeadLetter registration
appsettings.json                        ← EmailSettings, MessageBroker config
Data/ApplicationDbContext.cs           ← EF mappings for OutboxMessage, ProcessedMessage
```

---

## Configuration Reference

### appsettings.json

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
  },
  "MessageBroker": {
	"Idempotency": {
	  "EnableIdempotency": true,
	  "RetentionDays": 90
	},
	"DeadLetterNotification": {
	  "EmailRecipients": "admin@example.com;support@example.com"
	}
  }
}
```

### Program.cs Registration

```csharp
// Outbox and polling
builder.Services.AddMassTransit(x => {
	x.AddConsumer<BrokerMessageConsumer>();
	x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
});
builder.Services.AddMessageConsumers();
builder.Services.AddScoped<IOutboxService, OutboxService>();
builder.Services.AddScoped<IOutboxPublisher, OutboxPublisher>();
builder.Services.AddHostedService<OutboxPollingService>();

// Dead-letter notifications
builder.Services.AddScoped<IDeadLetterNotificationService, DeadLetterEmailNotificationService>();

// Idempotency
builder.Services.Configure<IdempotencySettings>(builder.Configuration.GetSection("MessageBroker:Idempotency"));
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
builder.Services.AddHostedService<IdempotencyCleanupService>();
```

---

## Status Summary

| Feature | Status | Files | Docs |
|---------|-------|-------|------|
| Outbox Pattern | ✅ | OutboxMessage, OutboxService, OutboxPollingService | implementation.md |
| Message Consumer Abstraction | ✅ | IMessageConsumer, MessageConsumerBase, BrokerMessageConsumer | implementation.md, reference.md |
| Idempotency | ✅ | ProcessedMessage, IdempotencyService | Idempotency.md |
| Dead Letter Handling | ✅ | OutboxMessageStatus, OutboxMessage.Status | DeadLetterHandling.md |
| Dead Letter Logging | ✅ | DeadLetterNotificationService | implementation.md |
| Dead Letter Email Alerts | ✅ | DeadLetterEmailNotificationService | DeadLetterEmailNotification.md, DEADLETTER_EMAIL_SETUP.md |
| RabbitMQ/Service Bus Transport | ⏳ | MassTransit transport config | implementation.md (Next Steps) |
| Processed Outbox Cleanup | ⏳ | Not yet implemented | implementation.md (Next Steps) |
| Multi-instance Row Locking | ⏳ | Not yet implemented | implementation.md (Next Steps) |

---

## Getting Started Checklist

### Initial Setup
- [ ] Review [implementation.md](implementation.md) overview
- [ ] Run database scripts: `DeadLetterIndex.sql`, `CreateProcessedMessageTable.sql`
- [ ] Verify MassTransit and Outbox registration in `Program.cs`
- [ ] Verify `EmailSettings` configured in `appsettings.json`

### Publishing Your First Message
- [ ] Create event class (e.g., `MyEventPayload`)
- [ ] Inject `IOutboxPublisher`
- [ ] Call `await publisher.PublishAsync(new MyEventPayload { ... })`
- [ ] Verify message appears in `OutboxMessage` table with Status=0
- [ ] Verify background polling processes it (Status changes to 2 after success)

### Creating Your First Consumer
- [ ] Create class: `class MyEventConsumer : MessageConsumerBase<MyEventPayload>`
- [ ] Implement `HandleAsync(payload, message, ct)`
- [ ] No manual registration needed
- [ ] Test by publishing matching event

### Enabling Dead-Letter Email Alerts
- [ ] Review [DeadLetterEmailNotification.md](DeadLetterEmailNotification.md)
- [ ] Add recipients to `MessageBroker:DeadLetterNotification:EmailRecipients`
- [ ] Verify `EmailSettings` is properly configured
- [ ] Done! Dead-letters will now send email notifications

### Testing Dead-Letter Scenario
- [ ] Create `TestDeadLetterConsumer : IMessageConsumer` that throws
- [ ] Publish a message matching that type
- [ ] Wait for polling (5 retries × ~15s intervals = ~75s)
- [ ] Verify: `OutboxMessage.Status = 3` (DeadLetter)
- [ ] Verify: Critical log entries mention the dead-letter
- [ ] Verify: Email notification received (if configured)

---

## Common Tasks

### Query Recent Dead-Lettered Messages
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
WHERE Status = 3
ORDER BY ProcessedAt DESC;
```

### Track a Message End-to-End
```sql
DECLARE @correlationId UNIQUEIDENTIFIER = 'your-correlation-id-here';

-- Find outbox message
SELECT * FROM dbo.OutboxMessage WHERE CorrelationId = @correlationId;

-- Find processed messages (idempotency records)
SELECT * FROM dbo.ProcessedMessage WHERE MessageId IN (
	SELECT Id FROM dbo.OutboxMessage WHERE CorrelationId = @correlationId
);
```

### Find Pending Messages Stuck in Retry Loop
```sql
SELECT 
	Id,
	MessageType,
	CorrelationId,
	CreatedAt,
	RetryCount,
	Exception
FROM dbo.OutboxMessage
WHERE Status = 1  -- Failed
  AND RetryCount >= 4  -- Close to dead-letter threshold
ORDER BY CreatedAt ASC;
```

### Manually Republish a Dead-Lettered Message
1. Fix the underlying issue (e.g., service connectivity, data validation)
2. Update the message in the database:
   ```sql
   UPDATE dbo.OutboxMessage
   SET Status = 0,  -- Reset to Pending
	   RetryCount = 0,
	   Exception = NULL,
	   ProcessedAt = NULL
   WHERE Id = 'message-id-here';
   ```
3. Wait for next polling cycle (~15 seconds)
4. Verify Status changes to 2 (Processed) or 3 (DeadLetter) based on outcome

---

## Troubleshooting

### Messages not being published?
- Check `OutboxMessage` table: are rows being inserted?
- Check if `OutboxPollingService` is running (look for logs)
- Verify MassTransit configuration in `Program.cs`
- Check for exceptions in logs or `Exception` column

### Dead-letter emails not sending?
- Verify `MessageBroker:DeadLetterNotification:EmailRecipients` is set
- Verify `EmailSettings` (Host, Port, Username, Password) is correct
- Check logs for email send errors
- Verify network connectivity to SMTP server
- Test email service independently (if possible)

### Duplicate message processing?
- Idempotency is enabled by default
- Check `ProcessedMessage` table for entries
- Verify `EnableIdempotency = true` in config
- Review [Idempotency.md](Idempotency.md) for detailed troubleshooting

### Performance issues?
- Polling interval: Check `OutboxPollingService.PollingInterval` (default 15s)
- Batch size: Check `OutboxPollingService.BatchSize` (default 50)
- Index coverage: Ensure `IX_OutboxMessage_Status_CreatedAt` exists
- Query slow logs to identify bottlenecks

---

## Next Steps & Future Work

- [ ] Replace in-memory MassTransit transport with RabbitMQ or Azure Service Bus
- [ ] Add cleanup job for processed (Status=2) outbox rows (archive or delete after N days)
- [ ] Implement distributed row locking for multiple app instances using SQL Server
- [ ] Add metrics/counters for messages published, processed, failed, and dead-lettered
- [ ] Implement message expiration (auto-deadletter if too old without processing)
- [ ] Add admin UI for querying, re-publishing, and monitoring dead-letter queue

---

## Support & Contribution

For questions, issues, or improvements:
1. Review the relevant documentation file listed above
2. Check the "Troubleshooting" section in that document
3. Review logs (check Serilog output in `Logs/` directory or Seq)
4. Examine the database state using provided SQL queries
5. Create/update unit tests in `Tests/Portal.Tests/Services/MessageBroker/`

