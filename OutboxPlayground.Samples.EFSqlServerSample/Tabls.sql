
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
    RiskAssessment nvarchar(20) NOT NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id])
);

-- Create MyOutbox table (CloudEvent outbox table)
CREATE TABLE [MyOutbox] (
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
    CONSTRAINT [PK_MyOutbox] PRIMARY KEY ([Id])
);

-- Add indexes for better performance
CREATE INDEX [IX_Payments_Status] ON [Payments] ([Status]);
CREATE INDEX [IX_Payments_CustomerId] ON [Payments] ([CustomerId]);
CREATE INDEX [IX_Payments_CreatedAt] ON [Payments] ([CreatedAt]);

CREATE INDEX [IX_MyOutbox_Type] ON [MyOutbox] ([Type]);
CREATE INDEX [IX_MyOutbox_Source] ON [MyOutbox] ([Source]);
CREATE INDEX [IX_MyOutbox_Time] ON [MyOutbox] ([Time]);

-- Add constraints for PaymentStatus enum values
ALTER TABLE [Payments] 
ADD CONSTRAINT [CK_Payments_Status] 
CHECK ([Status] IN ('Pending', 'Processing', 'Completed', 'Failed', 'Cancelled'));

-- Add constraints for RiskAssessment enum values
ALTER TABLE [Payments] 
ADD CONSTRAINT [CK_Payments_RiskAssessment] 
CHECK ([Status] IN ('Low', 'Medium', 'High'));

-- Add constraints for CloudEvent required fields
ALTER TABLE [MyOutbox] 
ADD CONSTRAINT [CK_MyOutbox_SpecVersion] 
CHECK ([SpecVersion] IS NOT NULL AND LEN([SpecVersion]) > 0);

ALTER TABLE [MyOutbox] 
ADD CONSTRAINT [CK_MyOutbox_Type] 
CHECK ([Type] IS NOT NULL AND LEN([Type]) > 0);

ALTER TABLE [MyOutbox] 
ADD CONSTRAINT [CK_MyOutbox_Source] 
CHECK ([Source] IS NOT NULL AND LEN([Source]) > 0);
