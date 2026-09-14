IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [Roles] (
        [Id] uniqueidentifier NOT NULL,
        [Name] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [Tenants] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [Alerts] (
        [Id] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NULL,
        [Type] nvarchar(max) NOT NULL,
        [Severity] int NOT NULL,
        [Status] int NOT NULL,
        [Title] nvarchar(max) NOT NULL,
        [PayloadJson] nvarchar(max) NOT NULL,
        [DetectedAt] datetimeoffset NOT NULL,
        [AcknowledgedAt] datetimeoffset NULL,
        [ResolvedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Alerts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Alerts_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NULL,
        [BranchId] uniqueidentifier NULL,
        [Action] nvarchar(max) NOT NULL,
        [ResourceType] nvarchar(max) NOT NULL,
        [ResourceId] nvarchar(max) NULL,
        [QueryId] nvarchar(max) NULL,
        [CorrelationId] nvarchar(max) NOT NULL,
        [MetadataJson] nvarchar(max) NOT NULL,
        [OccurredAt] datetimeoffset NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AuditLogs_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [Branches] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Code] nvarchar(450) NOT NULL,
        [Address] nvarchar(max) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Branches] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Branches_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [Customers] (
        [Id] uniqueidentifier NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Email] nvarchar(450) NULL,
        [PhoneMasked] nvarchar(max) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Customers_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [Products] (
        [Id] uniqueidentifier NOT NULL,
        [Sku] nvarchar(450) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [CurrentPrice] decimal(18,2) NOT NULL,
        [CurrentCost] decimal(18,2) NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Products_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] uniqueidentifier NOT NULL,
        [Email] nvarchar(320) NOT NULL,
        [DisplayName] nvarchar(max) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [EmployeeCode] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [Inventories] (
        [Id] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [QuantityOnHand] int NOT NULL,
        [SafetyStock] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Inventories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Inventories_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Inventories_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Inventories_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [UserBranches] (
        [UserId] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserBranches] PRIMARY KEY ([UserId], [BranchId]),
        CONSTRAINT [FK_UserBranches_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserBranches_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [UserRoles] (
        [UserId] uniqueidentifier NOT NULL,
        [RoleId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [Orders] (
        [Id] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [CustomerId] uniqueidentifier NULL,
        [EmployeeId] uniqueidentifier NOT NULL,
        [OrderNumber] nvarchar(450) NOT NULL,
        [Status] int NOT NULL,
        [OrderedAt] datetimeoffset NOT NULL,
        [Subtotal] decimal(18,2) NOT NULL,
        [DiscountAmount] decimal(18,2) NOT NULL,
        [TotalAmount] decimal(18,2) NOT NULL,
        [Currency] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Orders_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_Orders_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Orders_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [OrderItems] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [ProductId] uniqueidentifier NOT NULL,
        [Quantity] int NOT NULL,
        [UnitPrice] decimal(18,2) NOT NULL,
        [UnitCostAtSale] decimal(18,2) NOT NULL,
        [DiscountAmount] decimal(18,2) NOT NULL,
        [LineTotal] decimal(18,2) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_OrderItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrderItems_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [Payments] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [Method] int NOT NULL,
        [Status] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaidAt] datetimeoffset NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Payments_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Payments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE TABLE [Refunds] (
        [Id] uniqueidentifier NOT NULL,
        [OrderId] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [ReasonCode] nvarchar(max) NOT NULL,
        [RequestedAt] datetimeoffset NOT NULL,
        [CompletedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NOT NULL,
        [TenantId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_Refunds] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Refunds_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Refunds_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Alerts_TenantId_Status_DetectedAt] ON [Alerts] ([TenantId], [Status], [DetectedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_TenantId_OccurredAt] ON [AuditLogs] ([TenantId], [OccurredAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Branches_TenantId_Code] ON [Branches] ([TenantId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Customers_TenantId_Email] ON [Customers] ([TenantId], [Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_BranchId] ON [Employees] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_TenantId_EmployeeCode] ON [Employees] ([TenantId], [EmployeeCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Inventories_BranchId] ON [Inventories] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Inventories_ProductId] ON [Inventories] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Inventories_TenantId_BranchId_ProductId] ON [Inventories] ([TenantId], [BranchId], [ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrderItems_TenantId_OrderId] ON [OrderItems] ([TenantId], [OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_BranchId] ON [Orders] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_CustomerId] ON [Orders] ([CustomerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_EmployeeId] ON [Orders] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Orders_TenantId_BranchId_OrderedAt] ON [Orders] ([TenantId], [BranchId], [OrderedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Orders_TenantId_OrderNumber] ON [Orders] ([TenantId], [OrderNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_OrderId] ON [Payments] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Payments_TenantId] ON [Payments] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Products_TenantId_Sku] ON [Products] ([TenantId], [Sku]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Refunds_OrderId] ON [Refunds] ([OrderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Refunds_TenantId_BranchId_RequestedAt] ON [Refunds] ([TenantId], [BranchId], [RequestedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Roles_Name] ON [Roles] ([Name]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Tenants_Code] ON [Tenants] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserBranches_BranchId] ON [UserBranches] ([BranchId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Users_TenantId] ON [Users] ([TenantId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260914161316_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260914161316_InitialCreate', N'10.0.11');
END;

COMMIT;
GO

