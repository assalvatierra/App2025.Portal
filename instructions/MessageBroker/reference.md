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

## Technology Stack

- **.NET Version:** .NET 10
- **Serialization:** System.Text.Json
- **Messaging framework:** MassTransit 9.2.3 (in-memory transport for now; RabbitMQ/Azure Service Bus planned)
- **Outbox storage:** MS SQL Server via EF Core (`ApplicationDbContext.OutboxMessage`)
- **Nullable Reference Types:** Enabled

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
