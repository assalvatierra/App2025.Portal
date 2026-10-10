SET QUOTED_IDENTIFIER ON;

CREATE TABLE dbo.ProcessedMessage
(
	Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
	MessageId UNIQUEIDENTIFIER NOT NULL,
	ConsumerType NVARCHAR(512) NOT NULL,
	ProcessingStartedAt DATETIME2 NULL,
	ProcessedAt DATETIME2 NULL,
	ExpiresAt DATETIME2 NULL
);

CREATE UNIQUE INDEX UX_ProcessedMessage_Message_Consumer
ON dbo.ProcessedMessage (MessageId, ConsumerType);

CREATE INDEX IX_ProcessedMessage_ExpiresAt
ON dbo.ProcessedMessage (ExpiresAt);
