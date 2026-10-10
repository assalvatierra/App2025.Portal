# MessageBroker Implementation Guide

## Overview
The MessageBroker feature provides a publish-subscribe pattern for asynchronous communication between system components. This document outlines the implementation approach and architecture.

## Core Components

### 1. BrokerMessage Class
**Location:** `Services/MessageBroker/BrokerMessage.cs`

The `BrokerMessage` class serves as the standard message contract used by both publishers and consumers.

#### Properties:
- **Id** (Guid) - Unique identifier auto-generated for each message
- **MessageType** (string) - Category/type of the message (e.g., "OrderCreated", "InventoryUpdated")
- **Timestamp** (DateTime) - Creation time in UTC, auto-set by constructor
- **Payload** (string) - Serialized message content (typically JSON)
- **CorrelationId** (Guid?) - Optional identifier for tracking related messages across the system
- **Headers** (Dictionary<string, string>?) - Optional metadata for routing and tracing
- **PartitionKey** (string?) - Optional key for message queue partitioning (Redis, Kafka)

#### Constructors:
1. **BrokerMessage()** - Default constructor
   - Auto-generates `Id` as new Guid
   - Auto-sets `Timestamp` to `DateTime.UtcNow`

2. **BrokerMessage(string messageType, string payload)** - Convenience constructor
   - Sets message type and payload
   - Inherits default constructor initialization

## Design Pattern

The MessageBroker architecture follows these principles:

1. **Message Envelope Pattern** - BrokerMessage wraps all messaging content
2. **Publisher-Subscriber Pattern** - Decoupled communication between services
3. **Correlation Tracking** - CorrelationId enables end-to-end transaction tracing
4. **Partition-Based Ordering** - PartitionKey ensures message ordering for related messages

## JSON Serialization
Messages are serialized/deserialized using System.Text.Json for performance in .NET 10:

```csharp
// Publisher example
var message = new BrokerMessage("OrderCreated", JsonSerializer.Serialize(orderData))
{
    CorrelationId = correlationId,
    PartitionKey = customerId
};

// Consumer example
var brokerMessage = JsonSerializer.Deserialize<BrokerMessage>(messageJson);
var orderData = JsonSerializer.Deserialize<OrderDto>(brokerMessage.Payload);
```

## Outbox Pattern (Implemented)

Messages are written to the `OutboxMessage` table in the same `SaveChanges` as the business data, then published to MassTransit by a hosted service inside the Portal project. This hosting choice keeps deployment simple. The polling service can be moved to a separate worker project later.

### Files (all in `Services/MessageBroker/`)
- `OutboxMessage.cs` - EF entity (Id, MessageType, Payload, CorrelationId, PartitionKey, Headers, CreatedAt, ProcessedAt, RetryCount, Exception, Status)
- `IOutboxService.cs` / `OutboxService.cs` - AddAsync (no save), GetPendingAsync, MarkAsProcessedAsync, MarkAsFailedAsync, MarkAsDeadLetterAsync
- `IOutboxPublisher.cs` / `OutboxPublisher.cs` - publish API for application code, writes to the outbox
- `OutboxPollingService.cs` - BackgroundService; 15s interval, batch of 50, max 5 retries, publishes via `IPublishEndpoint`
- `OutboxCleanupSettings.cs` - Configuration class (EnableCleanup, RetentionDays)
- `OutboxCleanupService.cs` - BackgroundService; runs daily to delete processed messages older than retention period

### Other changes
- `Data/ApplicationDbContext.cs` - `DbSet<OutboxMessage>` and mapping (Status default Pending, composite index on Status + CreatedAt)
- `Program.cs` - `AddMassTransit` (in-memory transport), scoped `IOutboxService` / `IOutboxPublisher`, `AddHostedService<OutboxPollingService>`, configure `OutboxCleanupSettings`, `AddHostedService<OutboxCleanupService>`
- `Portal.csproj` - MassTransit package reference
- `appsettings.json` - `MessageBroker:OutboxCleanup` section (EnableCleanup, RetentionDays)
- `instructions/MessageBroker/CreateOutboxTable.sql` - MS SQL Server DDL
- `instructions/MessageBroker/DeadLetterIndex.sql` - SQL script to add Status column and indexes

### Flow
1. Business code calls `IOutboxPublisher.PublishAsync(...)`
2. The caller's `SaveChangesAsync` commits the business data and the outbox row together
3. `OutboxPollingService` reads unprocessed rows and publishes a `BrokerMessage` to MassTransit
4. Success sets `ProcessedAt` and `Status=Processed`; failure increments `RetryCount` and sets `Status=Failed`
5. On max retries (default 5), marks as `Status=DeadLetter` and invokes notification service
6. `OutboxCleanupService` runs daily and deletes `Status=Processed` rows older than the retention period (default 30 days)
7. Dead-lettered messages are retained indefinitely for audit and investigation


## Idempotency (Implemented)

Per-consumer idempotency prevents duplicate message processing using a database-level unique constraint.

### Files (all in `Services/MessageBroker/`)
- `ProcessedMessage.cs` - EF entity (Id, MessageId, ConsumerType, ProcessingStartedAt, ProcessedAt, ExpiresAt)
- `IIdempotencyService.cs` / `IdempotencyService.cs` - TryReserveAsync, MarkProcessedAsync, ReleaseReservationAsync, CheckIfProcessedAsync
- `IdempotencySettings.cs` - Configuration class (EnableIdempotency, RetentionDays)
- `IdempotencyCleanupService.cs` - BackgroundService; runs daily to delete expired records in batches (1,000 rows at a time) to minimize lock contention
- `Data/ApplicationDbContext.cs` - `DbSet<ProcessedMessage>` and mapping with unique composite index `(MessageId, ConsumerType)`
- `instructions/MessageBroker/CreateProcessedMessageTable.sql` - MS SQL Server DDL
- `instructions/MessageBroker/Idempotency.md` - Detailed documentation on design and behavior

### Other changes
- `Program.cs` - Configure `IdempotencySettings`, register `IIdempotencyService`, add `IdempotencyCleanupService` hosted service
- `appsettings.json` - `MessageBroker:Idempotency` section (EnableIdempotency, RetentionDays)
- `BrokerMessageConsumer.cs` - Inject `IIdempotencyService`, call `TryReserveAsync` before each consumer processes, `MarkProcessedAsync` on success, `ReleaseReservationAsync` on failure

### How It Works
1. When a message arrives, `BrokerMessageConsumer` attempts to reserve it for each consumer by inserting a `ProcessedMessage` row
2. If the INSERT succeeds (first time), processing continues; if it fails (unique constraint violation), the message is a duplicate and is skipped
3. On success, the record is updated with `ProcessedAt` and `ExpiresAt` (set to `ProcessedAt + RetentionDays`)
4. On failure, the reservation is released (row deleted) so the message can be retried later
5. A daily cleanup job deletes expired records (where `ExpiresAt <= now`) in batches to reclaim storage efficiently

See `instructions/MessageBroker/Idempotency.md` for detailed behavior and troubleshooting.


## Consumer Abstraction (Implemented)

Files in `Services/MessageBroker/`:
- `IMessageConsumer.cs` - `MessageType` plus `ConsumeAsync(BrokerMessage, CancellationToken)`
- `MessageConsumerBase.cs` - `MessageConsumerBase<TPayload>`: deserializes the payload; implement `HandleAsync(payload, message, ct)`. MessageType defaults to `typeof(TPayload).Name`, which matches `IOutboxPublisher.PublishAsync<T>`
- `BrokerMessageConsumer.cs` - the single MassTransit `IConsumer<BrokerMessage>`; dispatches to every `IMessageConsumer` whose MessageType matches, and logs a warning if none match
- `MessageBrokerServiceExtensions.cs` - `AddMessageConsumers()` registers all concrete `IMessageConsumer` types in the assembly (scoped)

`Program.cs` calls `x.AddConsumer<BrokerMessageConsumer>()` and `AddMessageConsumers()`. To handle a new event, add a class deriving from `MessageConsumerBase<TPayload>`. No further registration is needed. If a handler throws, MassTransit's retry/error queue handling applies.

### First event: ReservationVerified
- `Events/ReservationVerified.cs` - event payload (ReservationId, CustomerName, ContactEmail, TransactionType, DateReceived, PortalItem, ContactNo, jsonData, Status)
- `Consumers/ReservationVerifiedConsumer.cs` - `MessageConsumerBase<ReservationVerified>`; currently logs the event
- `Consumers/ReservationVerifiedConsumerEmailSender.cs` - `MessageConsumerBase<ReservationVerified>`; sends customer email notification by mapping the event to a `PortalReservation` domain model and calling `IReservationService.SendCustomerNotification()`
- `Consumers/ReservationVerifiedInternalEmailSender.cs` - `MessageConsumerBase<ReservationVerified>`; sends internal email notification to configured internal recipients by calling `IReservationService.SendInternalReservationNotification()`. This consumer operates independently of customer notifications, allowing internal teams to be notified separately.
- `Controllers/PortalReservationController.cs` - publishes `ReservationVerified` through `IOutboxPublisher` after OTP verification (with `saveChanges: true`). The existing direct email call remains unchanged, so multiple consumers can handle the same event independently.


## Dead Letter Handling (Implemented)

When outbox messages fail to publish after the maximum number of retries (default 5), they are moved to **dead-letter** status for investigation and alerting.

### Files (all in `Services/MessageBroker/`)
- `OutboxMessageStatus.cs` - Enum with states: Pending, Failed, Processed, DeadLetter
- `IDeadLetterNotificationService.cs` / `DeadLetterNotificationService.cs` - Logging-only implementation
- `DeadLetterEmailNotificationService.cs` - Email alerts with HTML formatting
- Modified: `OutboxMessage.cs` - Added `Status` property
- Modified: `OutboxService.cs` - Added `MarkAsDeadLetterAsync()`, updated `GetPendingAsync()` to filter by Status
- Modified: `OutboxPollingService.cs` - Detects max retries, marks as DeadLetter, triggers notification
- Modified: `ApplicationDbContext.cs` - Added Status default and composite index (Status, CreatedAt)

### Behavior
1. When a message fails, `RetryCount` is incremented and `Status` is set to Failed
2. When `RetryCount >= MaxRetries`, the message is marked as DeadLetter and `ProcessedAt` is set
3. `IDeadLetterNotificationService` is invoked (logging or email based on registered implementation)
4. Dead-lettered messages are retained indefinitely for audit and investigation

### Configuration
- **Logging only (default)**: No configuration needed; critical errors are logged
- **Email alerts**: 
  - Register `DeadLetterEmailNotificationService` in `Program.cs`
  - Configure `MessageBroker:DeadLetterNotification:EmailRecipients` in `appsettings.json`
  - See `instructions/MessageBroker/DeadLetterEmailNotification.md` for details

### Database Schema
- Added `Status` column (INT, default 0 = Pending)
- Created index: `IX_OutboxMessage_Status_CreatedAt`
- Run SQL script: `instructions/MessageBroker/DeadLetterIndex.sql`

See `instructions/MessageBroker/DeadLetterHandling.md` and `DeadLetterEmailNotification.md` for detailed behavior and troubleshooting.

## Next Steps
- [x] Implement outbox (entity, service, publisher, polling service)
- [x] Add MassTransit and register it
- [x] Add SQL script for the outbox table
- [x] Create the table `dbo.OutboxMessage` in the dev database (via `CreateOutboxTable.sql`; script now sets `QUOTED_IDENTIFIER ON`, required by the filtered index; run with `sqlcmd -I` if using the CLI). Do not add an EF migration for this table, as it would conflict.
- [x] Implement IMessageConsumer abstraction (see Consumer Abstraction section)
- [x] Create first concrete consumer: `ReservationVerifiedConsumer` (see below)
- [x] Add ReservationVerifiedConsumerEmailSender consumer to send customer emails via event-driven architecture
- [x] Add ReservationVerifiedInternalEmailSender consumer to send internal emails via event-driven architecture
- [ ] Add more consumers (e.g., additional transformations or notifications)
- [x] Add idempotency for message consumption (track processed message IDs to prevent duplicate handling)
- [x] Add dead letter handling for messages that exceed max retries
- [ ] Replace in-memory transport with RabbitMQ or Azure Service Bus
- [ ] Add cleanup job for processed outbox rows
- [ ] Handle multiple app instances (row locking) before scaling out
