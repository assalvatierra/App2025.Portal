# Idempotency for Message Consumption

This document describes the per-consumer idempotency implementation for the MessageBroker feature.

Overview
--------
Each concrete message consumer independently tracks which BrokerMessage IDs it has processed. This prevents duplicate processing when messages are redelivered or when multiple app instances are running.

Storage
-------
Processed messages are stored in the `dbo.ProcessedMessage` table. The table has a unique constraint on `(MessageId, ConsumerType)` to ensure a single reservation/processed record per consumer.

Behavior
--------
- When a message arrives, the broker attempts to reserve the message for the specific consumer by inserting a `ProcessedMessage` row. If the insert fails (unique constraint), the message is considered a duplicate and skipped.
- If reservation succeeds, the consumer processes the message. On success the record is updated with `ProcessedAt` and an `ExpiresAt` is set based on retention settings.
- If processing fails, the reservation is released (row deleted) so the message can be retried later.

Configuration
-------------
Settings are under `MessageBroker:Idempotency` in `appsettings.json`:

  "MessageBroker": {
	"Idempotency": {
	  "EnableIdempotency": true,
	  "RetentionDays": 90
	}
  }

Cleanup
-------
A future cleanup background service should delete `ProcessedMessage` records older than `ExpiresAt` to reclaim storage.

Notes
-----
- Idempotency is implemented at the broker/dispatch level (BrokerMessageConsumer) to avoid changing existing consumer constructors.
- The implementation uses the database unique constraint for correctness and to avoid race conditions.
