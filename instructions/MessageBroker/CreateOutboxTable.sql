-- Outbox table for the MessageBroker feature (MS SQL Server)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OutboxMessage' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
	CREATE TABLE dbo.OutboxMessage
	(
		Id            UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OutboxMessage PRIMARY KEY,
		MessageType   NVARCHAR(256)    NOT NULL,
		Payload       NVARCHAR(MAX)    NOT NULL,
		CorrelationId UNIQUEIDENTIFIER NULL,
		PartitionKey  NVARCHAR(256)    NULL,
		Headers       NVARCHAR(MAX)    NULL,
		CreatedAt     DATETIME2(7)     NOT NULL CONSTRAINT DF_OutboxMessage_CreatedAt DEFAULT (SYSUTCDATETIME()),
		ProcessedAt   DATETIME2(7)     NULL,
		RetryCount    INT              NOT NULL CONSTRAINT DF_OutboxMessage_RetryCount DEFAULT (0),
		Exception     NVARCHAR(MAX)    NULL
	);

	-- Supports the polling query: unprocessed messages ordered by CreatedAt
	CREATE NONCLUSTERED INDEX IX_OutboxMessage_Pending
		ON dbo.OutboxMessage (CreatedAt)
		INCLUDE (RetryCount)
		WHERE ProcessedAt IS NULL;

	CREATE NONCLUSTERED INDEX IX_OutboxMessage_ProcessedAt
		ON dbo.OutboxMessage (ProcessedAt);

	CREATE NONCLUSTERED INDEX IX_OutboxMessage_CorrelationId
		ON dbo.OutboxMessage (CorrelationId)
		WHERE CorrelationId IS NOT NULL;
END
GO

-- Sample: pending messages
-- SELECT TOP (50) * FROM dbo.OutboxMessage WHERE ProcessedAt IS NULL AND RetryCount < 5 ORDER BY CreatedAt;

-- Sample: cleanup of processed messages older than 7 days
-- DELETE FROM dbo.OutboxMessage WHERE ProcessedAt IS NOT NULL AND ProcessedAt < DATEADD(DAY, -7, SYSUTCDATETIME());
