# MessageBroker API Reference

## BrokerMessage Class

### Namespace
```csharp
Portal.Services.MessageBroker
```

### Class Definition
```csharp
public class BrokerMessage
{
    public Guid Id { get; set; }
    public string MessageType { get; set; }
    public DateTime Timestamp { get; set; }
    public string Payload { get; set; }
    public Guid? CorrelationId { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? PartitionKey { get; set; }
}
```

### Public Constructors

#### BrokerMessage()
**Description:** Initializes a new instance with auto-generated Id and current UTC timestamp.

**Parameters:** None

**Example:**
```csharp
var message = new BrokerMessage();
// Id: [auto-generated Guid]
// Timestamp: [current UTC time]
```

#### BrokerMessage(string messageType, string payload)
**Description:** Initializes a new instance with specified message type and payload.

**Parameters:**
- `messageType` (string) - The type/category of the message
- `payload` (string) - The serialized message content

**Example:**
```csharp
var message = new BrokerMessage("OrderCreated", jsonPayload);
```

## Properties Reference

| Property | Type | Nullable | Description |
|----------|------|----------|-------------|
| Id | Guid | No | Unique identifier (auto-generated) |
| MessageType | string | No | Message category for routing |
| Timestamp | DateTime | No | UTC creation time (auto-set) |
| Payload | string | No | Serialized message content |
| CorrelationId | Guid? | Yes | Related messages tracking ID |
| Headers | Dictionary<string, string>? | Yes | Metadata key-value pairs |
| PartitionKey | string? | Yes | Queue partitioning key |

## Usage Patterns

### Publisher Pattern
```csharp
// Create a message
var orderData = new { OrderId = 123, Amount = 99.99m };
var message = new BrokerMessage("OrderCreated", JsonSerializer.Serialize(orderData))
{
    CorrelationId = Guid.NewGuid(),
    PartitionKey = customerId.ToString()
};

// Publish to broker
await publisher.PublishAsync(message);
```

### Consumer Pattern
```csharp
// Receive message
BrokerMessage message = await consumer.ReceiveAsync();

// Extract and deserialize payload
var orderData = JsonSerializer.Deserialize<OrderDto>(message.Payload);

// Use correlation for tracking
LogError($"Processing failed for correlation: {message.CorrelationId}");
```

## Correlation Tracking

**Purpose:** Link related messages across multiple services for distributed transaction tracing.

**Example Workflow:**
```
Message 1: OrderCreated
├─ CorrelationId: ABC-123
├─ PartitionKey: CUST-001

Message 2: PaymentProcessed
├─ CorrelationId: ABC-123 (same!)
├─ PartitionKey: CUST-001

Message 3: InventoryUpdated
├─ CorrelationId: ABC-123 (same!)
├─ PartitionKey: CUST-001
```

All three messages connected via CorrelationId for end-to-end visibility.

## Related Interfaces

- `IOutboxPublisher` - Implemented. Stores messages in the outbox
- `IOutboxService` - Implemented. Outbox persistence operations
- `IMessageConsumer` - Implemented. Per-message-type handler, dispatched by `BrokerMessageConsumer`
- `IIdempotencyService` - Implemented. Per-consumer idempotency tracking to prevent duplicate processing

## Message Consumers

| Consumer | Location | Purpose | Event Type |
|----------|----------|---------|------------|
| ReservationVerifiedConsumer | `Services/MessageBroker/Consumers/ReservationVerifiedConsumer.cs` | Logs the ReservationVerified event | ReservationVerified |
| ReservationVerifiedConsumerEmailSender | `Services/MessageBroker/Consumers/ReservationVerifiedConsumerEmailSender.cs` | Sends email notification to customer after OTP verification | ReservationVerified |
| ReservationVerifiedInternalEmailSender | `Services/MessageBroker/Consumers/ReservationVerifiedInternalEmailSender.cs` | Sends email notification to configured internal recipients | ReservationVerified |

### ReservationVerifiedConsumerEmailSender

**Namespace:** `Portal.Services.MessageBroker.Consumers`

**Purpose:** Handles `ReservationVerified` events and sends customer notification emails via the `IReservationService`.

**Class Definition:**
```csharp
public class ReservationVerifiedConsumerEmailSender : MessageConsumerBase<ReservationVerified>
{
    private readonly ILogger<ReservationVerifiedConsumerEmailSender> _logger;
    private readonly IReservationService _reservationService;

    public ReservationVerifiedConsumerEmailSender(
        ILogger<ReservationVerifiedConsumerEmailSender> logger,
        IReservationService reservationService);

    protected override async Task HandleAsync(
        ReservationVerified payload, 
        BrokerMessage message, 
        CancellationToken cancellationToken);
}
```

**Description:** 
Consumes `ReservationVerified` events and:
1. Maps the event data to a `PortalReservation` domain model
2. Sends a customer notification email via `IReservationService.SendCustomerNotification()`
3. Logs the event with reservation ID, transaction type, customer name, message ID, and correlation ID

**Event Mapping:**
| ReservationVerified Property | PortalReservation Property |
|------------------------------|---------------------------|
| ReservationId | Id |
| TransactionType | TransactionType |
| PortalItem | PortalItemId |
| CustomerName | CustomerName |
| ContactNo | ContactNo |
| ContactEmail | ContactEmail |
| DateReceived | DateReceived |
| jsonData | JsonData |
| Status | Status |

### ReservationVerifiedInternalEmailSender

**Namespace:** `Portal.Services.MessageBroker.Consumers`

**Purpose:** Handles `ReservationVerified` events and sends internal email notifications to configured internal recipients.

**Class Definition:**
```csharp
public class ReservationVerifiedInternalEmailSender : MessageConsumerBase<ReservationVerified>
{
    private readonly ILogger<ReservationVerifiedInternalEmailSender> _logger;
    private readonly IReservationService _reservationService;

    public ReservationVerifiedInternalEmailSender(
        ILogger<ReservationVerifiedInternalEmailSender> logger,
        IReservationService reservationService);

    protected override async Task HandleAsync(
        ReservationVerified payload, 
        BrokerMessage message, 
        CancellationToken cancellationToken);
}
```

**Description:**
Consumes `ReservationVerified` events and:
1. Maps the event data to a `PortalReservation` domain model and wraps it in a list
2. Sends internal notification emails via `IReservationService.SendInternalReservationNotification()`
3. Logs the event with reservation ID, transaction type, customer name, message ID, and correlation ID

This consumer operates independently and runs in parallel with other consumers, allowing internal teams to receive notifications based on rules configured in the portal system configuration (email recipients, subject, title, and message template).

**Event Mapping:**
| ReservationVerified Property | PortalReservation Property |
|------------------------------|------------------------------|
| ReservationId | Id |
| TransactionType | TransactionType |
| PortalItem | PortalItemId |
| CustomerName | CustomerName |
| ContactNo | ContactNo |
| ContactEmail | ContactEmail |
| DateReceived | DateReceived |
| jsonData | JsonData |
| Status | Status |

## Technology Stack

- **.NET Version:** .NET 10
- **Serialization:** System.Text.Json
- **Messaging framework:** MassTransit 9.2.3 (in-memory transport for now; RabbitMQ/Azure Service Bus planned)
- **Outbox storage:** MS SQL Server via EF Core (`ApplicationDbContext.OutboxMessage`)
- **Idempotency storage:** MS SQL Server via EF Core (`ApplicationDbContext.ProcessedMessage`)
- **Nullable Reference Types:** Enabled

## Idempotency Service

Per-consumer duplicate detection using database-level unique constraints.

### IIdempotencyService Interface

**Namespace:** `Portal.Services.MessageBroker`

```csharp
public interface IIdempotencyService
{
    /// <summary>
    /// Attempts to reserve the message for processing by the specified consumer.
    /// Returns true if the reservation succeeded (caller should proceed with processing).
    /// Returns false if another consumer/instance already reserved or processed this message.
    /// </summary>
    Task<bool> TryReserveAsync(Guid messageId, string consumerType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a previously reserved message as processed successfully.
    /// </summary>
    Task MarkProcessedAsync(Guid messageId, string consumerType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a reservation if processing failed so the message can be retried later.
    /// </summary>
    Task ReleaseReservationAsync(Guid messageId, string consumerType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a message was already processed for the given consumer.
    /// </summary>
    Task<bool> CheckIfProcessedAsync(Guid messageId, string consumerType, CancellationToken cancellationToken = default);
}
```

### ProcessedMessage Entity

**Location:** `Services/MessageBroker/ProcessedMessage.cs`

**Table:** `dbo.ProcessedMessage`

| Column | Type | Nullable | Purpose |
|--------|------|----------|---------|
| `Id` | UNIQUEIDENTIFIER | No | Primary key (auto-generated) |
| `MessageId` | UNIQUEIDENTIFIER | No | The `BrokerMessage.Id` being tracked |
| `ConsumerType` | NVARCHAR(512) | No | Full type name of the consumer |
| `ProcessingStartedAt` | DATETIME2 | Yes | When the reservation was created (UTC) |
| `ProcessedAt` | DATETIME2 | Yes | When processing completed successfully |
| `ExpiresAt` | DATETIME2 | Yes | When this record expires for cleanup |

### Indexes

| Index | Type | Columns | Purpose |
|-------|------|---------|---------|
| UX_ProcessedMessage_Message_Consumer | UNIQUE | (MessageId, ConsumerType) | Enforces one record per consumer per message; enables duplicate detection |
| IX_ProcessedMessage_ExpiresAt | Standard | (ExpiresAt) | Enables fast cleanup of expired records |

### Configuration

Settings are under `MessageBroker:Idempotency` in `appsettings.json`:

```json
"MessageBroker": {
  "Idempotency": {
    "EnableIdempotency": true,
    "RetentionDays": 90
  }
}
```

**IdempotencySettings Class:**
```csharp
public class IdempotencySettings
{
    public bool EnableIdempotency { get; set; } = true;
    public int RetentionDays { get; set; } = 90;
}
```

### Usage Pattern

In `BrokerMessageConsumer`:

```csharp
foreach (var handler in handlers)
{
    var consumerType = handler.GetType().FullName ?? handler.GetType().Name;

    // Attempt to reserve the message for this consumer
    var reserved = await _idempotencyService.TryReserveAsync(message.Id, consumerType, context.CancellationToken);
    if (!reserved)
    {
        _logger.LogWarning("Skipping duplicate message {MessageId} for consumer {Consumer}", message.Id, consumerType);
        continue;
    }

    try
    {
        // Process the message
        await handler.ConsumeAsync(message, context.CancellationToken);

        // Mark as successfully processed
        await _idempotencyService.MarkProcessedAsync(message.Id, consumerType, context.CancellationToken);
    }
    catch (Exception ex)
    {
        // Release reservation so the message can be retried later
        _logger.LogError(ex, "Error processing message {MessageId} for consumer {Consumer}", message.Id, consumerType);
        await _idempotencyService.ReleaseReservationAsync(message.Id, consumerType, context.CancellationToken);
        throw;
    }
}
```

### IdempotencyCleanupService

**Purpose:** Background service that periodically deletes expired `ProcessedMessage` records.

**Behavior:**
- Runs every 24 hours
- Deletes all records where `ExpiresAt <= UtcNow`
- Logs count of deleted records
- Catches and logs any errors

**Registration in Program.cs:**
```csharp
builder.Services.AddHostedService<Portal.Services.MessageBroker.IdempotencyCleanupService>();
```

## Configuration Notes

- `Guid` properties default to `Guid.NewGuid()` in constructor
- `DateTime Timestamp` uses `DateTime.UtcNow` for consistency
- String properties default to `string.Empty`
- Nullable properties (`?`) allow null values for optional metadata


## Outbox Pattern (MassTransit)

All files live in `Services/MessageBroker/`.

| File | Purpose |
|------|---------|
| OutboxMessage.cs | EF Core entity (table `dbo.OutboxMessage`) |
| IOutboxService / OutboxService | Add, GetPending, MarkAsProcessed, MarkAsFailed |
| IOutboxPublisher / OutboxPublisher | Stores messages in the outbox, optionally saving |
| OutboxPollingService | BackgroundService publishing pending rows via MassTransit `IPublishEndpoint` (batch 50, 5s poll, 5 retries) |

SQL script: `instructions/MessageBroker/CreateOutboxTable.sql`. DbSet: `ApplicationDbContext.OutboxMessage`.

### Usage
```csharp
// inside a business operation: same SaveChanges commits business data and the outbox row
_db.PortalReservation.Add(reservation);
await _outboxPublisher.PublishAsync(new { reservation.Id }, messageType: "ReservationCreated");
await _db.SaveChangesAsync();
```

---

## Dead Letter Handling (NEW)

When messages fail to publish after \MaxRetries\ (default 5), they are moved to dead-letter status (\OutboxMessageStatus.DeadLetter\).

See [DeadLetterHandling.md](DeadLetterHandling.md), [DeadLetterEmailNotification.md](DeadLetterEmailNotification.md), and [README.md](README.md) for complete documentation.

Query: `SELECT * FROM dbo.OutboxMessage WHERE Status = 3 ORDER BY ProcessedAt DESC;`

