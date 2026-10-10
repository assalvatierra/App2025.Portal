# Dead Letter Handling

This document describes the dead-letter handling behavior added to the outbox/polling implementation.

Behavior summary:

- A new `Status` column was added to the `OutboxMessage` table. Values: Pending, Failed, Processed, DeadLetter.
- The polling service will attempt to publish pending messages. On failure it increments `RetryCount` and sets `Status = Failed`.
- When `RetryCount` reaches the configured max retries (currently 5 in the polling service), the message is moved to the DeadLetter state (`Status = DeadLetter`) and `ProcessedAt` is set to the time when it was dead-lettered.
- When a message is dead-lettered, the `IDeadLetterNotificationService` is invoked. The default implementation logs a critical event; you can replace or extend it to send emails or webhooks.
- Dead-lettered messages are retained indefinitely for audit and investigation.

Querying dead letters:

```sql
SELECT * FROM OutboxMessage WHERE Status = 3 -- DeadLetter
ORDER BY CreatedAt DESC;
```

Extending notifications:

Replace or decorate the `IDeadLetterNotificationService` registration in `Program.cs` to plug in email, webhooks, or alerting systems.
