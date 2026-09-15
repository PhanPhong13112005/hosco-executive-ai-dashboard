BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    CREATE TABLE [AlertRules] (
        [Id] uniqueidentifier NOT NULL,
        [Code] nvarchar(32) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [Severity] int NOT NULL,
        [IsEnabled] bit NOT NULL,
        [Threshold] decimal(18,2) NULL,
        [Baseline] decimal(18,2) NULL,
        [WindowMinutes] int NOT NULL,
        [CooldownMinutes] int NOT NULL,
        [BranchId] uniqueidentifier NULL,
        [ConfigJson] nvarchar(4000) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AlertRules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AlertRules_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    ALTER TABLE [Alerts] ADD [AcknowledgedBy] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    ALTER TABLE [Alerts] ADD [DedupKey] nvarchar(450) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    ALTER TABLE [Alerts] ADD [DetectedValue] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    ALTER TABLE [Alerts] ADD [Message] nvarchar(2000) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    ALTER TABLE [Alerts] ADD [ResolvedBy] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    ALTER TABLE [Alerts] ADD [RuleCode] nvarchar(32) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    ALTER TABLE [Alerts] ADD [RuleId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    ALTER TABLE [Alerts] ADD [ThresholdValue] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Alerts]') AND [c].[name] = N'Type');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Alerts] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Alerts] ALTER COLUMN [Type] nvarchar(64) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Alerts]') AND [c].[name] = N'Title');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Alerts] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Alerts] ALTER COLUMN [Title] nvarchar(250) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    UPDATE Alerts
    SET RuleCode = CASE Type
        WHEN 'cancellation-spike' THEN 'AL-01'
        WHEN 'revenue-drop' THEN 'AL-02'
        WHEN 'low-stock' THEN 'AL-03'
        WHEN 'employee-cancellation' THEN 'AL-04'
        WHEN 'abnormal-discount' THEN 'AL-05'
        ELSE 'LEGACY' END,
        Message = Title,
        DedupKey = LOWER(REPLACE(CONVERT(nvarchar(36), TenantId), '-', '')) + ':' +
            COALESCE(LOWER(REPLACE(CONVERT(nvarchar(36), BranchId), '-', '')), 'all') + ':' + Type + ':legacy'
    WHERE RuleCode = '';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    CREATE INDEX [IX_AlertRules_TenantId_BranchId_IsEnabled] ON [AlertRules] ([TenantId], [BranchId], [IsEnabled]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_AlertRules_TenantId_Code] ON [AlertRules] ([TenantId], [Code]) WHERE [TenantId] IS NOT NULL AND [Code] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    CREATE INDEX [IX_Alerts_RuleId] ON [Alerts] ([RuleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    CREATE INDEX [IX_Alerts_TenantId_BranchId_Severity_DetectedAt] ON [Alerts] ([TenantId], [BranchId], [Severity], [DetectedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    CREATE INDEX [IX_Alerts_TenantId_DedupKey_DetectedAt] ON [Alerts] ([TenantId], [DedupKey], [DetectedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    ALTER TABLE [Alerts] ADD CONSTRAINT [FK_Alerts_AlertRules_RuleId] FOREIGN KEY ([RuleId]) REFERENCES [AlertRules] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260915151000_AddDashboardAlertFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260915151000_AddDashboardAlertFoundation', N'10.0.11');
END;

COMMIT;
GO

