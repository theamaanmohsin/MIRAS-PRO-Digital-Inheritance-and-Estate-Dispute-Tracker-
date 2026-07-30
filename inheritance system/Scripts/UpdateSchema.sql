-- MirasPro: incremental schema patches (existing databases only)
-- For a FULL fresh database, use MirasPro_FullDatabase.sql instead.

USE [InheritanceDB];
GO

IF COL_LENGTH('Users', 'Phone') IS NULL
    ALTER TABLE Users ADD Phone NVARCHAR(50) NULL;

IF COL_LENGTH('Users', 'Cnic') IS NULL
    ALTER TABLE Users ADD Cnic NVARCHAR(20) NULL;

IF COL_LENGTH('Users', 'BarCouncilNumber') IS NULL
    ALTER TABLE Users ADD BarCouncilNumber NVARCHAR(50) NULL;

IF COL_LENGTH('Users', 'FirmName') IS NULL
    ALTER TABLE Users ADD FirmName NVARCHAR(200) NULL;

IF COL_LENGTH('Users', 'IsEditable') IS NULL
    ALTER TABLE Users ADD IsEditable BIT NOT NULL CONSTRAINT DF_Users_IsEditable DEFAULT 1;

IF COL_LENGTH('Disputes', 'AdminRejectionReason') IS NULL
    ALTER TABLE Disputes ADD AdminRejectionReason NVARCHAR(MAX) NULL;

IF COL_LENGTH('Disputes', 'AllowUserEdit') IS NULL
    ALTER TABLE Disputes ADD AllowUserEdit BIT NOT NULL CONSTRAINT DF_Disputes_AllowUserEdit DEFAULT 0;

IF COL_LENGTH('Disputes', 'ReviewedByAdminId') IS NULL
    ALTER TABLE Disputes ADD ReviewedByAdminId INT NULL;

IF COL_LENGTH('Disputes', 'ReviewedAt') IS NULL
    ALTER TABLE Disputes ADD ReviewedAt DATETIME2 NULL;

IF COL_LENGTH('Disputes', 'FiledBy') IS NULL
    ALTER TABLE Disputes ADD FiledBy INT NOT NULL CONSTRAINT DF_Disputes_FiledBy DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SystemSettings')
CREATE TABLE SystemSettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    [Key] NVARCHAR(100) NOT NULL,
    [Value] NVARCHAR(500) NOT NULL,
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedByAdminId INT NULL
);

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE [Key] = 'AdminAccessSecret')
INSERT INTO SystemSettings ([Key], [Value], UpdatedAt)
VALUES ('AdminAccessSecret', 'MirasPro@Admin2025', GETDATE());

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AdminAuditLogs')
CREATE TABLE AdminAuditLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AdminUserId INT NOT NULL,
    AdminName NVARCHAR(200) NOT NULL,
    Action NVARCHAR(100) NOT NULL,
    TargetType NVARCHAR(50) NULL,
    TargetId INT NULL,
    Details NVARCHAR(MAX) NULL,
    IpAddress NVARCHAR(50) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FrcDocuments')
BEGIN
    CREATE TABLE FrcDocuments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        FrcNumber NVARCHAR(100) NOT NULL,
        DocumentFilePath NVARCHAR(1000) NOT NULL,
        OriginalFileName NVARCHAR(500) NULL,
        FileSizeBytes BIGINT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        AdminNotes NVARCHAR(MAX) NULL,
        ReviewedByAdminId INT NULL,
        ReviewedAt DATETIME2 NULL,
        UploadedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_FrcDocuments_User FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
    );
END

UPDATE Users SET Role = 'User' WHERE Role IN ('Owner', 'LegalProfessional');
GO

PRINT 'Incremental schema update complete.';
GO
