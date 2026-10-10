-- SQL to add Status column and create indexes for outbox message queries
-- Run this against your database if you don't rely on EF migrations.

SET QUOTED_IDENTIFIER ON;

-- Step 1: Add the Status column if it doesn't exist
-- Status values: 0=Pending, 1=Failed, 2=Processed, 3=DeadLetter
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'OutboxMessage' AND COLUMN_NAME = 'Status'
)
BEGIN
    ALTER TABLE dbo.OutboxMessage
    ADD [Status] INT NOT NULL DEFAULT 0;

    PRINT 'Status column added to OutboxMessage table';
END
ELSE
BEGIN
    PRINT 'Status column already exists on OutboxMessage table';
END;

-- Step 2: Drop the old filtered index if it exists
IF EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_OutboxMessage_Pending' AND object_id = OBJECT_ID('dbo.OutboxMessage')
)
BEGIN
    DROP INDEX IX_OutboxMessage_Pending ON dbo.OutboxMessage;
    PRINT 'Dropped old IX_OutboxMessage_Pending index';
END;

-- Step 3: Create the new composite index on Status and CreatedAt
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_OutboxMessage_Status_CreatedAt' AND object_id = OBJECT_ID('dbo.OutboxMessage')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_OutboxMessage_Status_CreatedAt
    ON dbo.OutboxMessage([Status], [CreatedAt]);

    PRINT 'Created IX_OutboxMessage_Status_CreatedAt index';
END
ELSE
BEGIN
    PRINT 'Index IX_OutboxMessage_Status_CreatedAt already exists';
END;

-- Step 4 (optional): Create a filtered index for pending messages only (Status = 0)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes 
    WHERE name = 'IX_OutboxMessage_Pending_CreatedAt' AND object_id = OBJECT_ID('dbo.OutboxMessage')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_OutboxMessage_Pending_CreatedAt 
    ON dbo.OutboxMessage([CreatedAt]) 
    WHERE [Status] = 0;

    PRINT 'Created IX_OutboxMessage_Pending_CreatedAt filtered index';
END
ELSE
BEGIN
    PRINT 'Filtered index IX_OutboxMessage_Pending_CreatedAt already exists';
END;
