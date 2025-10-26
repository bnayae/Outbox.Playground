
-- SQL Script to create tables for PaymentDbContext and OutboxContext
-- Target: SQL Server

-- Create Payments table
CREATE TABLE [Payments] (
    [Id] uniqueidentifier NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Currency] nvarchar(3) NOT NULL,
    [PaymentMethod] nvarchar(50) NOT NULL,
    [CustomerId] uniqueidentifier NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [Status] nvarchar(20) NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id])
);

-- Create Users table
CREATE TABLE Users (
    [Id] uniqueidentifier NOT NULL,
    [Name] nvarchar(80) NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);

-- Create Outbox table (CloudEvent outbox table)
CREATE TABLE [Outbox] (
    [Id] nvarchar(450) NOT NULL,
    [SpecVersion] nvarchar(10) NOT NULL,
    [Type] nvarchar(255) NOT NULL,
    [Source] nvarchar(255) NOT NULL,
    [Time] datetimeoffset NOT NULL,
    [DataContentType] nvarchar(100) NULL,
    [DataSchema] nvarchar(500) NULL,
    [Subject] nvarchar(255) NULL,
    [Data] varbinary(max) NULL,
    [DataRef] nvarchar(1000) NULL,
    [TraceParent] varchar(55) NULL,
    CONSTRAINT [PK_Outbox] PRIMARY KEY ([Id])
);

-- Create HighRiskOutbox table (CloudEvent outbox table)
CREATE TABLE [HighRiskOutbox] (
    [Id] nvarchar(450) NOT NULL,
    [SpecVersion] nvarchar(10) NOT NULL,
    [Type] nvarchar(255) NOT NULL,
    [Source] nvarchar(255) NOT NULL,
    [Time] datetimeoffset NOT NULL,
    [DataContentType] nvarchar(100) NULL,
    [DataSchema] nvarchar(500) NULL,
    [Subject] nvarchar(255) NULL,
    [Data] varbinary(max) NULL,
    [DataRef] nvarchar(1000) NULL,
    [TraceParent] varchar(55) NULL,
    CONSTRAINT [PK_HighRiskOutbox] PRIMARY KEY ([Id])
);

-- Add indexes for better performance
CREATE INDEX [IX_Payments_Status] ON [Payments] ([Status]);
CREATE INDEX [IX_Payments_CustomerId] ON [Payments] ([CustomerId]);
CREATE INDEX [IX_Payments_CreatedAt] ON [Payments] ([CreatedAt]);

CREATE INDEX [IX_Outbox_Type] ON [Outbox] ([Type]);
CREATE INDEX [IX_Outbox_Source] ON [Outbox] ([Source]);
CREATE INDEX [IX_Outbox_Time] ON [Outbox] ([Time]);

CREATE INDEX [IX_HighRiskOutbox_Type] ON [HighRiskOutbox] ([Type]);
CREATE INDEX [IX_HighRiskOutbox_Source] ON [HighRiskOutbox] ([Source]);
CREATE INDEX [IX_HighRiskOutbox_Time] ON [HighRiskOutbox] ([Time]);

-- Add constraints for PaymentStatus enum values
ALTER TABLE [Payments] 
ADD CONSTRAINT [CK_Payments_Status] 
CHECK ([Status] IN ('Pending', 'Processing', 'Completed', 'Failed', 'Cancelled'));

-- Add constraints for CloudEvent required fields
ALTER TABLE [Outbox] 
ADD CONSTRAINT [CK_Outbox_SpecVersion] 
CHECK ([SpecVersion] IS NOT NULL AND LEN([SpecVersion]) > 0);

ALTER TABLE [Outbox] 
ADD CONSTRAINT [CK_Outbox_Type] 
CHECK ([Type] IS NOT NULL AND LEN([Type]) > 0);

ALTER TABLE [Outbox] 
ADD CONSTRAINT [CK_Outbox_Source] 
CHECK ([Source] IS NOT NULL AND LEN([Source]) > 0);

ALTER TABLE [HighRiskOutbox] 
ADD CONSTRAINT [CK_HighRiskOutbox_SpecVersion] 
CHECK ([SpecVersion] IS NOT NULL AND LEN([SpecVersion]) > 0);

ALTER TABLE [HighRiskOutbox] 
ADD CONSTRAINT [CK_HighRiskOutbox_Type] 
CHECK ([Type] IS NOT NULL AND LEN([Type]) > 0);

ALTER TABLE [HighRiskOutbox] 
ADD CONSTRAINT [CK_HighRiskOutbox_Source] 
CHECK ([Source] IS NOT NULL AND LEN([Source]) > 0);
