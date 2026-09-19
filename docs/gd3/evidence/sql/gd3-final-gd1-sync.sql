BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918161734_SyncFinalGd1BusinessRules'
)
BEGIN
    DROP INDEX [IX_AlertRules_TenantId_Code] ON [AlertRules];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918161734_SyncFinalGd1BusinessRules'
)
BEGIN
    ALTER TABLE [Products] ADD [FloorPrice] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918161734_SyncFinalGd1BusinessRules'
)
BEGIN
    ALTER TABLE [Products] ADD [IsKeySku] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918161734_SyncFinalGd1BusinessRules'
)
BEGIN
    ALTER TABLE [Inventories] ADD [ReservedQuantity] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918161734_SyncFinalGd1BusinessRules'
)
BEGIN
    ALTER TABLE [Alerts] ADD [BaselineValue] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918161734_SyncFinalGd1BusinessRules'
)
BEGIN
    ALTER TABLE [Alerts] ADD [ResolutionNote] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918161734_SyncFinalGd1BusinessRules'
)
BEGIN
    ALTER TABLE [Alerts] ADD [EscalatedAt] datetimeoffset NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918161734_SyncFinalGd1BusinessRules'
)
BEGIN
    CREATE TABLE [RefundItems] (
        [Id] uniqueidentifier NOT NULL,
        [RefundId] uniqueidentifier NOT NULL,
        [OrderItemId] uniqueidentifier NOT NULL,
        [Quantity] int NOT NULL,
        [ReturnedValue] decimal(18,2) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_RefundItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefundItems_OrderItems_OrderItemId] FOREIGN KEY ([OrderItemId]) REFERENCES [OrderItems] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RefundItems_Refunds_RefundId] FOREIGN KEY ([RefundId]) REFERENCES [Refunds] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RefundItems_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918161734_SyncFinalGd1BusinessRules'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AlertRules_TenantId_BranchId_Code] ON [AlertRules] ([TenantId], [BranchId], [Code]) WHERE [BranchId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918161734_SyncFinalGd1BusinessRules'
)
BEGIN
    CREATE INDEX [IX_RefundItems_OrderItemId] ON [RefundItems] ([OrderItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918161734_SyncFinalGd1BusinessRules'
)
BEGIN
    CREATE INDEX [IX_RefundItems_RefundId] ON [RefundItems] ([RefundId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918161734_SyncFinalGd1BusinessRules'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RefundItems_TenantId_RefundId_OrderItemId] ON [RefundItems] ([TenantId], [RefundId], [OrderItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260918161734_SyncFinalGd1BusinessRules'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260918161734_SyncFinalGd1BusinessRules', N'10.0.11');
END;

COMMIT;
GO

