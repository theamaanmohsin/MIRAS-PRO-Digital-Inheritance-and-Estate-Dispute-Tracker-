/*
================================================================================
  MirasPro — Complete SQL Server Database Script
  Matches the inheritance_system Blazor project (all tables + seed data)

  Default database: InheritanceDB
  Connection string: see appsettings.json → DefaultConnection

  RUN IN SSMS: Open this file → Execute (F5)
  Set @DropExisting = 1 below to wipe and recreate all tables (deletes all data).
================================================================================
*/

SET NOCOUNT ON;

/* ========== CONFIGURATION ========== */
DECLARE @DropExisting BIT = 1;  -- 1 = drop all tables first; 0 = keep data, create missing only

/* ========== CREATE DATABASE ========== */
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'InheritanceDB')
BEGIN
    CREATE DATABASE [InheritanceDB];
    PRINT 'Created database InheritanceDB';
END
GO

USE [InheritanceDB];
GO

/* ========== DROP TABLES (if @DropExisting = 1) ========== */
-- Run drop in one batch; edit @DropExisting in the block above, then re-run from USE [InheritanceDB]
IF 1 = 1  /* Change to IF 0 = 1 to skip drops */
BEGIN
    IF OBJECT_ID(N'dbo.AdminAuditLogs', N'U') IS NOT NULL DROP TABLE dbo.AdminAuditLogs;
    IF OBJECT_ID(N'dbo.FrcDocuments', N'U') IS NOT NULL DROP TABLE dbo.FrcDocuments;
    IF OBJECT_ID(N'dbo.AdminRegistrationInvites', N'U') IS NOT NULL DROP TABLE dbo.AdminRegistrationInvites;
    IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NOT NULL DROP TABLE dbo.SystemSettings;
    IF OBJECT_ID(N'dbo.Documents', N'U') IS NOT NULL DROP TABLE dbo.Documents;
    IF OBJECT_ID(N'dbo.Disputes', N'U') IS NOT NULL DROP TABLE dbo.Disputes;
    IF OBJECT_ID(N'dbo.Transfers', N'U') IS NOT NULL DROP TABLE dbo.Transfers;
    IF OBJECT_ID(N'dbo.Heirs', N'U') IS NOT NULL DROP TABLE dbo.Heirs;
    IF OBJECT_ID(N'dbo.InheritanceCases', N'U') IS NOT NULL DROP TABLE dbo.InheritanceCases;
    IF OBJECT_ID(N'dbo.Properties', N'U') IS NOT NULL DROP TABLE dbo.Properties;
    IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL DROP TABLE dbo.Users;
    PRINT 'Dropped existing tables.';
END
GO

/* ========== USERS ========== */
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId            INT IDENTITY(1,1) NOT NULL,
        FullName          NVARCHAR(200)     NOT NULL,
        Email             NVARCHAR(256)     NOT NULL,
        PasswordHash      NVARCHAR(500)     NOT NULL,
        Role              NVARCHAR(50)      NOT NULL,
        Phone             NVARCHAR(50)      NULL,
        Cnic              NVARCHAR(20)      NULL,
        BarCouncilNumber  NVARCHAR(50)      NULL,
        FirmName          NVARCHAR(200)     NULL,
        CreatedAt         DATETIME2(0)      NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        IsEditable        BIT               NOT NULL CONSTRAINT DF_Users_IsEditable DEFAULT (1),
        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId),
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );
    CREATE INDEX IX_Users_Role ON dbo.Users (Role);
END
GO

/* ========== PROPERTIES ========== */
IF OBJECT_ID(N'dbo.Properties', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Properties
    (
        PropertyId      INT IDENTITY(1,1) NOT NULL,
        OwnerId         INT               NOT NULL,
        Title           NVARCHAR(300)     NOT NULL,
        PropertyType    NVARCHAR(100)     NOT NULL,
        Location        NVARCHAR(500)     NOT NULL,
        EstimatedValue  DECIMAL(18,2)     NOT NULL CONSTRAINT DF_Properties_EstimatedValue DEFAULT (0),
        CreatedAt       DATETIME2(0)      NOT NULL CONSTRAINT DF_Properties_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Properties PRIMARY KEY CLUSTERED (PropertyId),
        CONSTRAINT FK_Properties_Users FOREIGN KEY (OwnerId) REFERENCES dbo.Users (UserId) ON DELETE CASCADE
    );
    CREATE INDEX IX_Properties_OwnerId ON dbo.Properties (OwnerId);
END
GO

/* ========== HEIRS ========== */
IF OBJECT_ID(N'dbo.Heirs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Heirs
    (
        HeirId        INT IDENTITY(1,1) NOT NULL,
        PropertyId    INT               NOT NULL,
        FullName      NVARCHAR(200)     NOT NULL,
        Relation      NVARCHAR(100)     NOT NULL,
        SharePercent  DECIMAL(9,4)      NOT NULL CONSTRAINT DF_Heirs_SharePercent DEFAULT (0),
        CreatedAt     DATETIME2(0)      NOT NULL CONSTRAINT DF_Heirs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Heirs PRIMARY KEY CLUSTERED (HeirId),
        CONSTRAINT FK_Heirs_Properties FOREIGN KEY (PropertyId) REFERENCES dbo.Properties (PropertyId) ON DELETE CASCADE
    );
    CREATE INDEX IX_Heirs_PropertyId ON dbo.Heirs (PropertyId);
END
GO

/* ========== INHERITANCE CASES ========== */
IF OBJECT_ID(N'dbo.InheritanceCases', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InheritanceCases
    (
        CaseId      INT IDENTITY(1,1) NOT NULL,
        PropertyId  INT               NOT NULL,
        Status      NVARCHAR(50)      NULL CONSTRAINT DF_InheritanceCases_Status DEFAULT (N'Active'),
        CreatedAt   DATETIME2(0)      NOT NULL CONSTRAINT DF_InheritanceCases_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_InheritanceCases PRIMARY KEY CLUSTERED (CaseId),
        CONSTRAINT FK_InheritanceCases_Properties FOREIGN KEY (PropertyId) REFERENCES dbo.Properties (PropertyId) ON DELETE CASCADE
    );
    CREATE INDEX IX_InheritanceCases_PropertyId ON dbo.InheritanceCases (PropertyId);
END
GO

/* ========== DOCUMENTS ========== */
IF OBJECT_ID(N'dbo.Documents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Documents
    (
        DocumentId   INT IDENTITY(1,1) NOT NULL,
        PropertyId   INT               NOT NULL,
        UploadedBy   INT               NOT NULL,
        FileName     NVARCHAR(500)     NOT NULL,
        FilePath     NVARCHAR(1000)    NOT NULL,
        UploadedAt   DATETIME2(0)      NOT NULL CONSTRAINT DF_Documents_UploadedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Documents PRIMARY KEY CLUSTERED (DocumentId),
        CONSTRAINT FK_Documents_Properties FOREIGN KEY (PropertyId) REFERENCES dbo.Properties (PropertyId) ON DELETE CASCADE,
        CONSTRAINT FK_Documents_Users FOREIGN KEY (UploadedBy) REFERENCES dbo.Users (UserId)
    );
    CREATE INDEX IX_Documents_PropertyId ON dbo.Documents (PropertyId);
END
GO

/* ========== DISPUTES ========== */
IF OBJECT_ID(N'dbo.Disputes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Disputes
    (
        DisputeId              INT IDENTITY(1,1) NOT NULL,
        PropertyId             INT               NOT NULL,
        FiledBy                INT               NOT NULL,
        DisputeType            NVARCHAR(100)     NOT NULL,
        Description            NVARCHAR(MAX)     NOT NULL,
        Status                 NVARCHAR(50)      NOT NULL CONSTRAINT DF_Disputes_Status DEFAULT (N'Pending'),
        CreatedAt              DATETIME2(0)      NOT NULL CONSTRAINT DF_Disputes_CreatedAt DEFAULT (SYSUTCDATETIME()),
        AdminRejectionReason   NVARCHAR(MAX)     NULL,
        AllowUserEdit          BIT               NOT NULL CONSTRAINT DF_Disputes_AllowUserEdit DEFAULT (0),
        ReviewedByAdminId      INT               NULL,
        ReviewedAt             DATETIME2(0)      NULL,
        CONSTRAINT PK_Disputes PRIMARY KEY CLUSTERED (DisputeId),
        CONSTRAINT FK_Disputes_Properties FOREIGN KEY (PropertyId) REFERENCES dbo.Properties (PropertyId) ON DELETE CASCADE,
        CONSTRAINT FK_Disputes_FiledBy FOREIGN KEY (FiledBy) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_Disputes_ReviewedByAdmin FOREIGN KEY (ReviewedByAdminId) REFERENCES dbo.Users (UserId)
    );
    CREATE INDEX IX_Disputes_PropertyId ON dbo.Disputes (PropertyId);
    CREATE INDEX IX_Disputes_Status ON dbo.Disputes (Status);
END
GO

/* ========== TRANSFERS ========== */
IF OBJECT_ID(N'dbo.Transfers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transfers
    (
        TransferId    INT IDENTITY(1,1) NOT NULL,
        PropertyId    INT               NOT NULL,
        FromUserId    INT               NOT NULL,
        ToUserId      INT               NOT NULL,
        TransferType  NVARCHAR(100)     NOT NULL,
        Status        NVARCHAR(50)      NOT NULL,
        CreatedAt     DATETIME2(0)      NOT NULL CONSTRAINT DF_Transfers_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Transfers PRIMARY KEY CLUSTERED (TransferId),
        CONSTRAINT FK_Transfers_Properties FOREIGN KEY (PropertyId) REFERENCES dbo.Properties (PropertyId) ON DELETE CASCADE,
        CONSTRAINT FK_Transfers_FromUser FOREIGN KEY (FromUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT FK_Transfers_ToUser FOREIGN KEY (ToUserId) REFERENCES dbo.Users (UserId)
    );
    CREATE INDEX IX_Transfers_PropertyId ON dbo.Transfers (PropertyId);
END
GO

/* ========== FRC DOCUMENTS ========== */
IF OBJECT_ID(N'dbo.FrcDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FrcDocuments
    (
        Id                  INT IDENTITY(1,1) NOT NULL,
        UserId              INT               NOT NULL,
        FrcNumber           NVARCHAR(100)     NOT NULL,
        DocumentFilePath    NVARCHAR(1000)    NOT NULL,
        OriginalFileName    NVARCHAR(500)     NULL,
        FileSizeBytes       BIGINT            NULL,
        Status              NVARCHAR(50)      NOT NULL CONSTRAINT DF_FrcDocuments_Status DEFAULT (N'Pending'),
        AdminNotes          NVARCHAR(MAX)     NULL,
        ReviewedByAdminId   INT               NULL,
        ReviewedAt          DATETIME2(0)      NULL,
        UploadedAt          DATETIME2(0)      NOT NULL CONSTRAINT DF_FrcDocuments_UploadedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2(0)      NOT NULL CONSTRAINT DF_FrcDocuments_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_FrcDocuments PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_FrcDocuments_User FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId) ON DELETE CASCADE,
        CONSTRAINT FK_FrcDocuments_ReviewedByAdmin FOREIGN KEY (ReviewedByAdminId) REFERENCES dbo.Users (UserId)
    );
    CREATE INDEX IX_FrcDocuments_UserId ON dbo.FrcDocuments (UserId);
    CREATE INDEX IX_FrcDocuments_Status ON dbo.FrcDocuments (Status);
END
GO

/* ========== ADMIN AUDIT LOGS ========== */
IF OBJECT_ID(N'dbo.AdminAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminAuditLogs
    (
        Id            INT IDENTITY(1,1) NOT NULL,
        AdminUserId   INT               NOT NULL,
        AdminName     NVARCHAR(200)     NOT NULL,
        Action        NVARCHAR(100)     NOT NULL,
        TargetType    NVARCHAR(50)      NULL,
        TargetId      INT               NULL,
        Details       NVARCHAR(MAX)     NULL,
        IpAddress     NVARCHAR(50)      NULL,
        CreatedAt     DATETIME2(0)      NOT NULL CONSTRAINT DF_AdminAuditLogs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AdminAuditLogs PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AdminAuditLogs_AdminUser FOREIGN KEY (AdminUserId) REFERENCES dbo.Users (UserId)
    );
    CREATE INDEX IX_AdminAuditLogs_CreatedAt ON dbo.AdminAuditLogs (CreatedAt DESC);
END
GO

/* ========== ADMIN REGISTRATION INVITES (optional) ========== */
IF OBJECT_ID(N'dbo.AdminRegistrationInvites', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminRegistrationInvites
    (
        Id                  INT IDENTITY(1,1) NOT NULL,
        SecureInviteToken   NVARCHAR(200)     NOT NULL,
        RecipientEmail      NVARCHAR(256)     NOT NULL,
        IsUsed              BIT               NOT NULL CONSTRAINT DF_AdminRegistrationInvites_IsUsed DEFAULT (0),
        CreatedByAdminId    INT               NULL,
        UsedByUserId        INT               NULL,
        ExpiresAt           DATETIME2(0)      NOT NULL,
        CreatedAt           DATETIME2(0)      NOT NULL CONSTRAINT DF_AdminRegistrationInvites_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UsedAt              DATETIME2(0)      NULL,
        CONSTRAINT PK_AdminRegistrationInvites PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_AdminRegistrationInvites_Token UNIQUE (SecureInviteToken)
    );
END
GO

/* ========== SYSTEM SETTINGS ========== */
IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemSettings
    (
        Id                  INT IDENTITY(1,1) NOT NULL,
        [Key]               NVARCHAR(100)     NOT NULL,
        [Value]             NVARCHAR(500)     NOT NULL,
        UpdatedAt           DATETIME2(0)      NOT NULL CONSTRAINT DF_SystemSettings_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedByAdminId    INT               NULL,
        CONSTRAINT PK_SystemSettings PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_SystemSettings_Key UNIQUE ([Key])
    );
END
GO

/* ========== SEED DATA ========== */
IF NOT EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE [Key] = N'AdminAccessSecret')
BEGIN
    INSERT INTO dbo.SystemSettings ([Key], [Value], UpdatedAt)
    VALUES (N'AdminAccessSecret', N'MirasPro@Admin2025', SYSUTCDATETIME());
END
GO

PRINT '========================================';
PRINT ' MirasPro database ready: InheritanceDB';
PRINT ' Admin secret key: MirasPro@Admin2025';
PRINT ' Register at /register (User or Administrator)';
PRINT '========================================';
GO
