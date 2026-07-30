using InheritanceSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace inheritance_system.Services
{
    /// <summary>
    /// Safe, idempotent patches for databases that already exist (no drop/recreate).
    /// </summary>
    public static class DatabaseSchemaInitializer
    {
        public static async Task EnsureAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider
                .GetService<ILoggerFactory>()?
                .CreateLogger(typeof(DatabaseSchemaInitializer));

            try
            {
                await db.Database.EnsureCreatedAsync();

                if (!await db.Database.CanConnectAsync())
                {
                    logger?.LogWarning(
                        "Cannot connect to SQL Server. Update DefaultConnection in appsettings.json.");
                    return;
                }

                foreach (var sql in GetStatements())
                {
                    try
                    {
                        await db.Database.ExecuteSqlRawAsync(sql);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning("Schema patch skipped: {Message}", ex.Message);
                    }
                }

                logger?.LogInformation("Database schema check completed.");
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Database schema initialization could not complete.");
            }
        }

        private static IEnumerable<string> GetStatements() =>
        [
            // Legacy table name used in older projects
            """
            IF OBJECT_ID(N'dbo.Cases', N'U') IS NOT NULL
               AND OBJECT_ID(N'dbo.InheritanceCases', N'U') IS NULL
                EXEC sp_rename N'dbo.Cases', N'InheritanceCases';
            """,

            "IF COL_LENGTH('Users', 'Phone') IS NULL ALTER TABLE Users ADD Phone NVARCHAR(50) NULL;",
            "IF COL_LENGTH('Users', 'Cnic') IS NULL ALTER TABLE Users ADD Cnic NVARCHAR(20) NULL;",
            "IF COL_LENGTH('Users', 'BarCouncilNumber') IS NULL ALTER TABLE Users ADD BarCouncilNumber NVARCHAR(50) NULL;",
            "IF COL_LENGTH('Users', 'FirmName') IS NULL ALTER TABLE Users ADD FirmName NVARCHAR(200) NULL;",
            """
            IF COL_LENGTH('Users', 'IsEditable') IS NULL
                ALTER TABLE Users ADD IsEditable BIT NOT NULL CONSTRAINT DF_Users_IsEditable DEFAULT 1;
            """,

            "IF COL_LENGTH('Disputes', 'AdminRejectionReason') IS NULL ALTER TABLE Disputes ADD AdminRejectionReason NVARCHAR(MAX) NULL;",
            "IF COL_LENGTH('Disputes', 'ReviewedByAdminId') IS NULL ALTER TABLE Disputes ADD ReviewedByAdminId INT NULL;",
            "IF COL_LENGTH('Disputes', 'ReviewedAt') IS NULL ALTER TABLE Disputes ADD ReviewedAt DATETIME2 NULL;",

            """
            IF COL_LENGTH('Disputes', 'AllowUserEdit') IS NULL
                ALTER TABLE Disputes ADD AllowUserEdit BIT NOT NULL CONSTRAINT DF_Disputes_AllowUserEdit DEFAULT 0;
            """,

            // FiledBy: safe for existing rows (backfill from property owner)
            """
            IF COL_LENGTH('Disputes', 'FiledBy') IS NULL
            BEGIN
                ALTER TABLE Disputes ADD FiledBy INT NULL;
                UPDATE d
                SET d.FiledBy = p.OwnerId
                FROM Disputes d
                INNER JOIN Properties p ON d.PropertyId = p.PropertyId;
                UPDATE Disputes
                SET FiledBy = (SELECT TOP 1 UserId FROM Users ORDER BY UserId)
                WHERE FiledBy IS NULL;
                ALTER TABLE Disputes ALTER COLUMN FiledBy INT NOT NULL;
            END
            """,

            """
            IF COL_LENGTH('Documents', 'UploadedBy') IS NULL
            BEGIN
                ALTER TABLE Documents ADD UploadedBy INT NULL;
                UPDATE d SET d.UploadedBy = p.OwnerId
                FROM Documents d INNER JOIN Properties p ON d.PropertyId = p.PropertyId;
                UPDATE Documents SET UploadedBy = (SELECT TOP 1 UserId FROM Users ORDER BY UserId) WHERE UploadedBy IS NULL;
                ALTER TABLE Documents ALTER COLUMN UploadedBy INT NOT NULL;
            END
            """,

            // ── NEW: Document review columns ──────────────────────────────
            "IF COL_LENGTH('Documents', 'Status') IS NULL ALTER TABLE Documents ADD Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Documents_Status DEFAULT 'Pending';",
            "IF COL_LENGTH('Documents', 'AdminNotes') IS NULL ALTER TABLE Documents ADD AdminNotes NVARCHAR(MAX) NULL;",
            "IF COL_LENGTH('Documents', 'ReviewedByAdminId') IS NULL ALTER TABLE Documents ADD ReviewedByAdminId INT NULL;",
            "IF COL_LENGTH('Documents', 'ReviewedAt') IS NULL ALTER TABLE Documents ADD ReviewedAt DATETIME2 NULL;",

            """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SystemSettings')
            CREATE TABLE SystemSettings (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                [Key] NVARCHAR(100) NOT NULL,
                [Value] NVARCHAR(500) NOT NULL,
                UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                UpdatedByAdminId INT NULL
            );
            """,

            """
            IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE [Key] = 'AdminAccessSecret')
            INSERT INTO SystemSettings ([Key], [Value], UpdatedAt)
            VALUES ('AdminAccessSecret', 'MirasPro@Admin2025', GETDATE());
            """,

            """
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
            """,

            """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'FrcDocuments')
            CREATE TABLE FrcDocuments (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                UserId INT NOT NULL,
                FrcNumber NVARCHAR(100) NOT NULL,
                DocumentFilePath NVARCHAR(1000) NOT NULL,
                OriginalFileName NVARCHAR(500) NULL,
                FileSizeBytes BIGINT NULL,
                Status NVARCHAR(50) NOT NULL CONSTRAINT DF_FrcDocuments_Status DEFAULT 'Pending',
                AdminNotes NVARCHAR(MAX) NULL,
                ReviewedByAdminId INT NULL,
                ReviewedAt DATETIME2 NULL,
                UploadedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                CONSTRAINT FK_FrcDocuments_User FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
            );
            """,

            """
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InheritanceCases')
            CREATE TABLE InheritanceCases (
                CaseId INT IDENTITY(1,1) PRIMARY KEY,
                PropertyId INT NOT NULL,
                Status NVARCHAR(50) NULL CONSTRAINT DF_InheritanceCases_Status DEFAULT 'Active',
                CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                CONSTRAINT FK_InheritanceCases_Properties FOREIGN KEY (PropertyId) REFERENCES Properties(PropertyId) ON DELETE CASCADE
            );
            """,

            "UPDATE Users SET Role = 'User' WHERE Role IN ('Owner', 'LegalProfessional');"
        ];
    }
}