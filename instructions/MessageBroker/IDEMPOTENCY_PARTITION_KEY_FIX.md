# Idempotency & Partition Key Integration - Implementation Update

## 🎯 Problem Statement

When a message was skipped due to partition key mismatch:
- `BrokerMessageConsumer` still marked the message as **MarkProcessedAsync**
- This prevented other consumers (with matching partition keys) from processing the message
- Idempotency record was created even though message wasn't actually processed

**Expected Behavior:**
- If partition key doesn't match → Skip without marking as processed
- Message stays available for other consumers to process
- Idempotency reservation is released

---

## 🔧 Solution Implemented

### 1. **Updated IMessageConsumer Interface**

Changed `ConsumeAsync` to return `Task<bool>` instead of `Task`:

```csharp
// BEFORE:
Task ConsumeAsync(BrokerMessage message, CancellationToken cancellationToken = default);

// AFTER:
/// <summary>
/// Returns true if processed, false if skipped (e.g., due to partition key mismatch).
/// </summary>
Task<bool> ConsumeAsync(BrokerMessage message, CancellationToken cancellationToken = default);
```

**Rationale:** Enable consumers to signal back whether the message was actually processed or skipped.

---

### 2. **Updated MessageConsumerBase**

Modified `ConsumeAsync` to return:
- **`false`** if partition key doesn't match (message skipped)
- **`true`** if partition key match or no partition filtering (message processed)

```csharp
public async Task<bool> ConsumeAsync(BrokerMessage message, CancellationToken cancellationToken = default)
{
	if (_assignedPartitionKeys is not null && _assignedPartitionKeys.Count > 0)
	{
		var messagePartitionKey = message.PartitionKey;
		var isMatch = !string.IsNullOrEmpty(messagePartitionKey) && 
					  _assignedPartitionKeys.Contains(messagePartitionKey);

		if (!isMatch)
		{
			System.Diagnostics.Debug.WriteLine($"[PartitionKeyFilter] Message {message.Id}: SKIPPED");
			return false;  // ← Return false = was not processed
		}
	}

	var payload = JsonSerializer.Deserialize<TPayload>(message.Payload) 
		?? throw new InvalidOperationException(...);

	await HandleAsync(payload, message, cancellationToken);
	return true;  // ← Return true = was processed
}
```

---

### 3. **Updated BrokerMessageConsumer**

Changed message processing logic to:
1. Call `ConsumeAsync` and capture return value
2. **If processed (`true`):** Mark as processed (`MarkProcessedAsync`)
3. **If skipped (`false`):** Release reservation (`ReleaseReservationAsync`)

```csharp
try
{
	// ConsumeAsync returns true if processed, false if skipped
	var wasProcessed = await handler.ConsumeAsync(message, context.CancellationToken);

	if (wasProcessed)
	{
		// Only mark as processed if actually handled
		await _idempotencyService.MarkProcessedAsync(message.Id, consumerType, context.CancellationToken);
	}
	else
	{
		// Message was skipped (e.g., partition key mismatch)
		// Release reservation so other consumers can process it
		await _idempotencyService.ReleaseReservationAsync(message.Id, consumerType, context.CancellationToken);
	}
}
catch (Exception ex)
{
	// Release reservation on error
	await _idempotencyService.ReleaseReservationAsync(message.Id, consumerType, context.CancellationToken);
	throw;
}
```

---

## 📊 Message Flow with Partition Key Handling

```
┌─────────────────────────────────────────────────────────┐
│ Message arrives with partitionKey="0"                   │
└──────────────────────┬──────────────────────────────────┘
					   │
					   ▼
┌─────────────────────────────────────────────────────────┐
│ BrokerMessageConsumer                                   │
│                                                         │
│ TryReserveAsync(message_id, "Consumer1")               │
│ → Reserve = true (first time seeing this message)      │
└──────────────────────┬──────────────────────────────────┘
					   │
					   ▼
┌─────────────────────────────────────────────────────────┐
│ ReservationVerifiedConsumer                             │
│ (assigned partition key: "1")                           │
│                                                         │
│ ConsumeAsync():                                         │
│  ├─ Check: message.partitionKey ("0") in ["1"]?        │
│  ├─ Result: NO MATCH                                   │
│  └─ return false ← Was SKIPPED                         │
└──────────────────────┬──────────────────────────────────┘
					   │
					   ▼
┌─────────────────────────────────────────────────────────┐
│ BrokerMessageConsumer (back)                            │
│                                                         │
│ wasProcessed = false                                   │
│                                                         │
│ ├─ Do NOT call MarkProcessedAsync ✓                   │
│ │  (Message NOT counted as processed)                  │
│                                                         │
│ └─ ReleaseReservationAsync() ✓                         │
│    (Reservation released, other consumers can try)     │
└──────────────────────┬──────────────────────────────────┘
					   │
					   ▼
┌─────────────────────────────────────────────────────────┐
│ Other Consumers (next cycle)                            │
│                                                         │
│ ReservationVerifiedConsumerEmailSender                  │
│ (assigned partition key: "0")                           │
│                                                         │
│ TryReserveAsync(message_id, "ConsumerEmailSender")     │
│ → Reserve = true (previous reservation was released)   │
│                                                         │
│ ConsumeAsync():                                         │
│  ├─ Check: message.partitionKey ("0") in ["0"]?        │
│  ├─ Result: MATCH ✓                                    │
│  └─ Process message & return true                      │
│                                                         │
│ MarkProcessedAsync() ✓                                │
│ (Message counted as processed by this consumer)        │
└─────────────────────────────────────────────────────────┘
```

---

## 🔄 Idempotency Behavior

### Before Fix:
| Step | Action | State |
|------|--------|-------|
| 1 | Reserve "Consumer1" | Reserved |
| 2 | Message skipped (partition mismatch) | ← ❌ Bug: still marked as processed |
| 3 | MarkProcessedAsync called | Processed |
| 4 | Other consumers try | Rejected (already processed) |

### After Fix:
| Step | Action | State |
|------|--------|-------|
| 1 | Reserve "Consumer1" | Reserved |
| 2 | Message skipped (partition mismatch) | ← ✅ Reservation released |
| 3 | ReleaseReservationAsync called | Available |
| 4 | Other consumers try | Can reserve & process |

---

## 📝 Debug Output

When running with debug diagnostics, you'll see:

**Partition key mismatch (skipped):**
```
[PartitionKeyFilter] Message ID {guid}: partitionKey='0', assignedKeys=[1], match=False
[PartitionKeyFilter] Message ID {guid}: SKIPPED - partition key mismatch
[BrokerMessageConsumer] Message {guid}: Released reservation for ReservationVerifiedConsumer (skipped due to partition key)
```

**Partition key match (processed):**
```
[PartitionKeyFilter] Message ID {guid}: partitionKey='0', assignedKeys=[0], match=True
[PartitionKeyFilter] Message ID {guid}: Message will be processed
(HandleAsync completes)
[BrokerMessageConsumer] Message {guid}: Marked as processed for ReservationVerifiedConsumerEmailSender
```

---

## ✅ Files Modified

1. **Services/MessageBroker/IMessageConsumer.cs**
   - Changed `ConsumeAsync` return type from `Task` to `Task<bool>`
   - Added documentation about return meaning

2. **Services/MessageBroker/MessageConsumerBase.cs**
   - Changed `ConsumeAsync` to return `bool`
   - Returns `false` on partition key mismatch
   - Returns `true` after `HandleAsync` completes

3. **Services/MessageBroker/BrokerMessageConsumer.cs**
   - Captures `wasProcessed` return value
   - Conditional logic: `MarkProcessedAsync` only if `wasProcessed == true`
   - Calls `ReleaseReservationAsync` if `wasProcessed == false`
   - Added diagnostic logging for release events

---

## 🚀 Testing the Fix

### Scenario: Even ID Reservation (partitionKey="0")

1. **With Config:**
   ```json
   "PartitionKeyAssignments": {
	 "ReservationVerifiedConsumer": "1",
	 "ReservationVerifiedConsumerEmailSender": "0",
	 "ReservationVerifiedInternalEmailSender": "1"
   }
   ```

2. **Expected Behavior:**
   - ReservationVerifiedConsumer: SKIPS (not assigned key "0")
   - ReservationVerifiedConsumerEmailSender: PROCESSES (assigned key "0")
   - ReservationVerifiedInternalEmailSender: SKIPS (not assigned key "0")

3. **Verify in Output Window:**
   - Should see "SKIPPED - partition key mismatch" for first and third consumer
   - Should see message processing for second consumer
   - Should see idempotency marks only for processedconsumer

---

## 🎯 Benefits

1. **Correct Idempotency:** Only actual processing creates idempotency records
2. **Partition-Aware:** Multiple consumers can process same message type for different partitions
3. **Message Distribution:** Messages aren't lost or duplicated across partition boundaries
4. **Reservation Efficiency:** Failed matches don't waste reservation slots
5. **Diagnostic Visibility:** Clear logging of what happened to each message

---
