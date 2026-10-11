# Partition Key Handling - Debug Analysis Report

## 🔴 Issue Identified

**Messages with partition key "0" are being processed by consumers assigned to partition key "1"**

### Reported Behavior
- Configuration defines PartitionKeyAssignments with `"ReservationVerifiedConsumer": "1"`
- Controller publishes messages with `partitionKey: (reservations.Id%2).ToString()` → produces "0" or "1"
- Expected: Messages with partition key "0" should be **skipped**
- Actual: Messages with partition key "0" are being **processed**

---

## 🔍 Root Cause Analysis

### Configuration Mismatch
```
appsettings.json (Lines 46-50):
┌─────────────────────────────────────────────┐
│ "PartitionKeyAssignments": {                │
│   "ReservationVerifiedConsumer": "1",       │ ← Assigned key: "1"
│   "ReservationVerifiedConsumerEmailSender": │
│   "ReservationVerifiedInternalEmailSender": │
│ }                                           │
└─────────────────────────────────────────────┘

PortalReservationController.cs (Line 412):
Publish with: (reservations.Id % 2).ToString()
├─ Even Id → "0"  ← Should be REJECTED by consumer assigned to "1"
└─ Odd  Id → "1"  ← Should be ACCEPTED
```

### Configuration Reading Issue

The `ParseAssignedKeys()` method in all three consumers was observed to return **`null`** even though configuration defined key "1":

```csharp
// Current behavior (BROKEN):
private static IEnumerable<string>? ParseAssignedKeys(IConfiguration configuration, string consumerName)
{
	var section = configuration.GetSection("PartitionKeyAssignments");
	var value = section[consumerName];  // ← Returns NULL despite config having "1"
	if (string.IsNullOrWhiteSpace(value)) 
		return null;  // ← Returns null early
	return value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
}
```

### Filter Logic Issue

In `MessageConsumerBase.ConsumeAsync()`:
```csharp
// When _assignedPartitionKeys is NULL, this condition is FALSE:
if (_assignedPartitionKeys is not null && _assignedPartitionKeys.Count > 0)
{
	// PARTITION KEY FILTERING IS BYPASSED
}
// → Consumer processes ALL messages (no filtering)
```

---

## 📊 Message Flow Breakdown

### Scenario: Reservation with Even ID (Id % 2 = 0)

```
┌───────────────────────────────────┐
│ PortalReservationController       │
│ publishes message                 │
│   partitionKey: "0"               │
└─────────────┬─────────────────────┘
			  │
			  ▼
┌───────────────────────────────────┐
│ OutboxPollingService              │
│ (picks up pending message)        │
└─────────────┬─────────────────────┘
			  │
			  ▼
┌───────────────────────────────────┐
│ BrokerMessageConsumer             │
│ (MassTransit dispatcher)          │
└─────────────┬─────────────────────┘
			  │
			  ▼
┌───────────────────────────────────────────────────────┐
│ ReservationVerifiedConsumer                           │
│                                                       │
│ Configuration: "PartitionKeyAssignments": "1"        │
│ Message:      partitionKey = "0"                     │
│                                                       │
│ ParseAssignedKeys() returns: NULL ❌                 │
│ _assignedPartitionKeys: NULL                         │
│                                                       │
│ ConsumeAsync() check:                                │
│ if (_assignedPartitionKeys is not null && ...) → FALSE
│                                                       │
│ ▶ PROCESSES MESSAGE (should skip!) ❌               │
└───────────────────────────────────────────────────────┘
```

---

## ✅ Solutions Implemented

### 1. Enhanced Diagnostics
Added `System.Diagnostics.Debug.WriteLine()` logging to:
- **ParseAssignedKeys()** - All three consumers
  - Logs: `section.Path`, `value`, parsed keys
  - Identifies if configuration is loaded correctly

- **MessageConsumerBase.ConsumeAsync()** 
  - Logs: `messagePartitionKey`, `assignedKeys`, match decision
  - Shows when messages are accepted/skipped

### 2. Improved Filter Logic
```csharp
// BEFORE:
if (string.IsNullOrEmpty(messagePartitionKey) || !_assignedPartitionKeys.Contains(messagePartitionKey))
{
	return;  // Skip
}

// AFTER:
var isMatch = !string.IsNullOrEmpty(messagePartitionKey) && _assignedPartitionKeys.Contains(messagePartitionKey);
if (!isMatch)
{
	System.Diagnostics.Debug.WriteLine($"SKIPPED - partition key mismatch");
	return;
}
```

### 3. Clearer State Reporting
- When no assignment exists: Logs "No partition key assigned (config value is null/empty)"
- When consumer has no filtering: Logs "will process all messages"
- For each message: Shows assigned vs message keys and decision

---

## 🔧 How to Use Debug Output

Run the application and check the Visual Studio **Output Window** (Debug channel):

```
[PartitionKeyFilter] ReservationVerifiedConsumer: section.Path='PartitionKeyAssignments', value='1'
[PartitionKeyFilter] ReservationVerifiedConsumer: Assigned keys = 1

[PartitionKeyFilter] Message ID {guid}: partitionKey='0', assignedKeys=[1], match=False
[PartitionKeyFilter] Message ID {guid}: SKIPPED - partition key mismatch

[PartitionKeyFilter] Message ID {guid}: partitionKey='1', assignedKeys=[1], match=True
[PartitionKeyFilter] Message ID {guid}: Message processed
```

---

## 🎯 Next Steps

1. **Run with debug output enabled** - Check Output Window for diagnostic messages
2. **Verify configuration is loaded** - Confirm ParseAssignedKeys logs show correct values
3. **Verify message filtering** - Confirm ConsumeAsync logs show partition key matching
4. **Adjust configuration if needed**:
   ```json
   "PartitionKeyAssignments": {
	 "ReservationVerifiedConsumer": "0,1",              // Both partitions
	 "ReservationVerifiedConsumerEmailSender": "0",    // Even IDs only
	 "ReservationVerifiedInternalEmailSender": "1"     // Odd IDs only
   }
   ```

---

## Files Modified

1. `Services/MessageBroker/MessageConsumerBase.cs` - Enhanced logging in ConsumeAsync
2. `Services/MessageBroker/Consumers/ReservationVerifiedConsumer.cs` - Config read diagnostics
3. `Services/MessageBroker/Consumers/ReservationVerifiedConsumerEmailSender.cs` - Config read diagnostics
4. `Services/MessageBroker/Consumers/ReservationVerifiedInternalEmailSender.cs` - Config read diagnostics
